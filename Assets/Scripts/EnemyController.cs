using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyController : MonoBehaviour
{
    [Header("Suspicion")] 
    public float suspicionIncreaseSpeed = 0.5f;
    public float suspicionDecreaseSpeed = 0.03f;

    public float suspicionToInvestigate = 0.5f;
    public float suspicionToChase = 1f;

    public float suspicion;

    private bool heardNoise;


    [Header("Investigation")]
    [SerializeField] private float investigateDuration = 10f;
    [SerializeField] private float lookAroundSpeed = 120f;
    public float investigateTimer;
    public bool isInvestigating;
    private Vector3 lastHeardPosition;
    private Vector3 lastSeenPosition;
    
    private bool waitingAtInvestigatePoint;
    private float investigatePointTimer;
    private Vector3 investigateCenter;
    [SerializeField] private float investigateRadius = 4f; // радиус поиска вокруг центра расследования
    [SerializeField] private float investigatePointWaitTime = 0.8f; // сколько стоять/оглядываться на точке
    [SerializeField] private float navSampleMaxDistance = 2f; // насколько далеко искать NavMesh точку от кандидата


    [Header("FOV")] 
    public float viewRadiusDark = 8f;
    public float viewRadiusLight = 12f;

    public float viewAngleDark = 90f;
    public float viewAngleLight = 120f;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("LightMapping")] 
    public float lightMin = 0.15f;
    public float lightMax = 0.75f;
    public float lightGamma = 0.7f;

    [Header("PlayerLinks")] 
    public Transform player;
    public LightDetector playerLight;

    [Header("Health")] 
    public float maxHp = 100f;
    public float hp;
    public bool isDead;
    [SerializeField] private Animator animator;
    //todo: ragdoll

    private static readonly int HitTrig = Animator.StringToHash("Hit");
    private static readonly int DieTrig = Animator.StringToHash("Die");

    [Header("Patrol")] 
    public Transform[] patrolPoints;
    public float patrolSpeed = 3f;
    public float waitTimeAtPoint = 2f;


    [Header("Chase")] 
    public float chaseSpeed = 4f;

    private bool isChasing;

    private int currentPointIndex = 0;
    private NavMeshAgent agent;
    private float waitTimer;

    enum EnemyState
    {
        Patrol,
        Investigate,
        Chase
    }

    private EnemyState currentState = EnemyState.Patrol;
    private EnemyState previousState = EnemyState.Patrol;


    private void Start()
    {

        hp = maxHp;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    void Update()
    {
        if (isDead) return;
        bool canSeePlayer = CanSeePlayer();

        UpdateSuspicion(canSeePlayer, heardNoise);
        EvaluateSuspicion();

        // Если состояние поменялось — делаем действия входа (например, задать destination)
        if (currentState != previousState)
        {
            OnEnterState(currentState);
            previousState = currentState;
        }
        
        heardNoise = false;
        
        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;

            case EnemyState.Investigate:
                Investigate();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;
        }
    } 
    
    private void OnEnterState(EnemyState newState)
    {
        switch (newState)
        {
            case EnemyState.Patrol:
                isChasing = false;
                isInvestigating = false;
                waitingAtInvestigatePoint = false;
                investigatePointTimer = 0f;
                investigateTimer = 0f;

                agent.speed = patrolSpeed;
                agent.isStopped = false;

                if (patrolPoints != null && patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[currentPointIndex].position);
                break;

            case EnemyState.Investigate:
                isChasing = false;
                isInvestigating = true;

                agent.speed = patrolSpeed;
                agent.isStopped = false;

                investigateTimer = 0f;
                waitingAtInvestigatePoint = false;
                investigatePointTimer = 0f;

                // Сначала попробуем пойти к центру, чтобы выглядело естественно
                agent.SetDestination(investigateCenter);
                break;

            case EnemyState.Chase:
                isChasing = true;
                isInvestigating = false;
                waitingAtInvestigatePoint = false;
                investigatePointTimer = 0f;
                investigateTimer = 0f;

                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;
        }
    }
    
    void UpdateSuspicion(bool canSeePlayer, bool hearsNoise)
    {
        if (canSeePlayer)
        {
            suspicion += 1f;
            lastSeenPosition = player.position;
        }
        else if (hearsNoise)
        {
            suspicion += 0.1f;
            lastHeardPosition = player.position;
        }
        else
        {
            suspicion -= suspicionDecreaseSpeed * Time.deltaTime;
        }


        if (isInvestigating)
        {
            suspicion = Mathf.Max(suspicion, 0.5f);
        }

        if (isChasing)
        {
            suspicion = Mathf.Max(suspicion, 0.5f);
        }

        suspicion = Mathf.Clamp01(suspicion);
    }

    void EvaluateSuspicion()
    {

        if (isChasing)
            return;

        if (suspicion >= suspicionToChase)
        {
            currentState = EnemyState.Chase;
            return;
        }

        if (isInvestigating)
            return;

        if (suspicion >= suspicionToInvestigate)
        {
            currentState = EnemyState.Investigate;
            return;
        }

        currentState = EnemyState.Patrol;
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;

        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }


    void Investigate()
    {
        isInvestigating = true;
        isChasing = false;
        agent.speed = patrolSpeed;
        
        investigateTimer += Time.deltaTime;

        if (investigateTimer >= investigateDuration)
        {
            isInvestigating = false;
            investigateTimer = 0f;
            waitingAtInvestigatePoint = false;
            investigatePointTimer = 0f;

            currentState = EnemyState.Patrol;
            return;
        }

        // Если дошли до текущей точки расследования
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (!waitingAtInvestigatePoint)
            {
                waitingAtInvestigatePoint = true;
                investigatePointTimer = 0f;
            }

            LookAround();
            investigatePointTimer += Time.deltaTime;

            if (investigatePointTimer >= investigatePointWaitTime)
            {
                waitingAtInvestigatePoint = false;
                investigatePointTimer = 0f;

                PickNewInvestigateDestination();
            }
        }
    }
    
    private void PickNewInvestigateDestination()
    {

        for (int i = 0; i < 8; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * investigateRadius;
            Vector3 candidate = investigateCenter + new Vector3(rnd.x, 0f, rnd.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navSampleMaxDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
        
        agent.SetDestination(investigateCenter);
    }

    void LookAround()
    {

        transform.Rotate(Vector3.up, lookAroundSpeed * Time.deltaTime);

    }

    void UpdatePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTimeAtPoint)
            {
                GoToNextPoint();
                waitTimer = 0f;
            }
        }
    }

    void UpdateChase()
    {
        isInvestigating = false;
        investigateTimer = 0f;
        //lostSightTimer = 0f; todo:доделать
        if (CanSeePlayer())
        {
            agent.speed = chaseSpeed;
            lastSeenPosition = player.position;
            agent.SetDestination(player.position);
        }
        else
            //todo: зафиксировать?
        {

            currentState = EnemyState.Investigate;
            agent.SetDestination(lastSeenPosition);

        }
    }


    bool CanSeePlayer()
    {
        float viewRadius, viewAngle;
        GetVisionFromLight(out viewRadius, out viewAngle);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > viewRadius)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);


        if (angleToPlayer > viewAngle / 2)
            return false;

        Debug.DrawRay
            (transform.position + Vector3.up, directionToPlayer * distanceToPlayer, Color.red);

        if (Physics.Raycast(
                transform.position + Vector3.up,
                directionToPlayer,
                distanceToPlayer,
                obstacleMask))
        {
            return false;
        }

        return true;
    }

    private void GetVisionFromLight(out float viewRadius, out float viewAngle)
    {
        float lightLevel = playerLight.currentLightLevel;
        ;

        // делаем 0..1 внутри диапазона [lightMin..lightMax]
        float t = Mathf.InverseLerp(lightMin, lightMax, lightLevel); // часть пройденного пути
        t = Mathf.Clamp01(t);

        // делаем "кривую" (опционально, но полезно)
        t = Mathf.Pow(t, lightGamma);

        viewRadius = Mathf.Lerp(viewRadiusDark, viewRadiusLight, t);
        viewAngle = Mathf.Lerp(viewAngleDark, viewAngleLight, t);
    }
    public void HearNoise(Vector3 noisePosition, float noiseRadius)
    {
        if (isDead) return;
        
        if (currentState == EnemyState.Chase)
            return;

        lastHeardPosition = noisePosition;
        investigateCenter = noisePosition;
        heardNoise = true;

        // Если мы уже расследуем — просто "перенацелимся" и продлим расследование
        if (currentState == EnemyState.Investigate)
        {
            investigateTimer = 0f;
            waitingAtInvestigatePoint = false;
            investigatePointTimer = 0f;
            agent.isStopped = false;
            PickNewInvestigateDestination();
            return;
        }

        // Иначе переходим в Investigate — OnEnterState задаст новую точку
        investigateTimer = 0f;
        waitingAtInvestigatePoint = false;
        investigatePointTimer = 0f;
        agent.isStopped = false;

        currentState = EnemyState.Investigate;
    }
    /*public void HearNoise(Vector3 noisePosition, float noiseRadius)
    {

        if (currentState == EnemyState.Chase)
            return;


        lastHeardPosition = noisePosition;
        heardNoise = true;

        investigateTimer = 0f;
        agent.isStopped = false;

        if (currentState == EnemyState.Investigate)
        {
            agent.SetDestination(lastHeardPosition);
        }
    }*/

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDir)
    {
        if (isDead) return;

        hp -= amount;

        if (animator != null)
            animator.SetTrigger(HitTrig);

        if (hp <= 0f)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null)
            animator.SetTrigger(DieTrig);

        //todo: ragdoll падение (после анимации?)
        Destroy(gameObject);
    }

    void OnDrawGizmos()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            Gizmos.color = Color.blue;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                    continue;

                Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
            }

            Gizmos.color = Color.red;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Transform current = patrolPoints[i];
                Transform next = patrolPoints[(i + 1) % patrolPoints.Length];

                if (current == null || next == null)
                    continue;

                Gizmos.DrawLine(current.position, next.position);
            }
        }

        Vector3 DirFromAngle(float angle)
        {
            float rad = (transform.eulerAngles.y + angle) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
        }

        void OnDrawGizmosSelected()
        {
            float viewRadius, viewAngle;
            GetVisionFromLight(out viewRadius, out viewAngle);

            Vector3 leftBoundary = DirFromAngle(-viewAngle / 2);
            Vector3 rightBoundary = DirFromAngle(viewAngle / 2);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

            if (player != null)
            {
                Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, player.position);
            }

            if (currentState == EnemyState.Investigate)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawSphere(lastSeenPosition, 0.2f);
            }

            if (currentState == EnemyState.Investigate)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lastHeardPosition, 0.2f);
            }
        }
    
}
