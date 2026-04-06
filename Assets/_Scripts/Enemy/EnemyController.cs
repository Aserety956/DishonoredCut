using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Suspicion")]
    //public float suspicionIncreaseSpeed = 0.5f;
    public float suspicionDecreaseSpeed = 0.03f;

    public float suspicionToInvestigate = 0.5f;
    public float suspicionToChase = 1f;
    public float suspicion;
    [SerializeField] private float suspicionGainNear; //в сек
    [SerializeField] private float suspicionGainFar; 
    
    [SerializeField] private float suspicionNearDist; 
    [SerializeField] private float suspicionFarDist; 

    //[SerializeField] private AnimationCurve distanceToGain;
    private bool heardNoise;


    [Header("Investigation")] [SerializeField]
    private float investigateDuration = 10f;

    //[SerializeField] private float lookAroundLength = 6.333f;
    public float investigateTimer;
    public bool isInvestigating;
    private Vector3 _lastHeardPosition;
    private Vector3 _lastSeenPosition;
    private bool _isWaitingAtInvestigatePoint;
    private float _investigatePointTimer;
    private Vector3 _investigateCenter;
    [SerializeField] private float investigateRadius = 4f; // радиус поиска вокруг центра расследования
    [SerializeField] private float investigatePointWaitTime = 0.8f; // сколько стоять/оглядываться на точке
    [SerializeField] private float navSampleMaxDistance = 2f; // насколько далеко искать NavMesh точку от кандидата
    
    [Header("Sounds")]
    [SerializeField] private SoundData investigateSounds;
    
    
    [Header("Ragdoll")] public bool _knocked;
    [SerializeField] private EnemyRagdoll ragdoll;

    public event Action OnEnemyDead;
    public event Action OnEnemyKnocked;

    [Header("Animations")] 
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Tooltip("Сколько секунд сглаживать изменение Speed параметра.")] [SerializeField]
    private float dampTime = 0.1f;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Lookbool = Animator.StringToHash("LookingAround");
    private static readonly int HitTrig = Animator.StringToHash("Hit");
    private static readonly int DieTrig = Animator.StringToHash("Die");


    [Header("FOV")] public float viewRadiusDark = 8f;
    public float viewRadiusLight = 12f;
    public float viewAngleDark = 90f;
    public float viewAngleLight = 120f;
    public LayerMask obstacleMask;
    public Transform viewSource;

    [Header("LightMapping")] public float lightMin = 0.15f;
    public float lightMax = 0.8f;
    //public float lightGamma = 0.7f;

    [Header("PlayerLinks")] public Transform player;
    public LightDetector playerLight;

    [Header("Health")] public float maxHp = 500f;
    public float hp;
    public bool isDead;
    

    [Header("Check")] [SerializeField] private float suspicionToСheck = 0.25f;
    private bool _isGoingToCheck;
    public bool IsChecking;

    [Header("Patrol")] public Transform[] patrolPoints;
    public float patrolSpeed = 3f;
    public float waitTimeAtPoint = 2f;


    [Header("Chase")] public float chaseSpeed = 4f;
    public bool isChasing;
    [SerializeField] private float chaseDuration;
    [SerializeField] private float chaseTimer;

    private int currentPointIndex = 0;
    private float waitTimer;

    private enum EnemyState
    {
        Patrol,
        Check,
        Investigate,
        Chase,
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
        AnimUpdate();

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

            case EnemyState.Check:
                UpdateCheck();
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
                IsChecking = false;

                _isWaitingAtInvestigatePoint = false;
                _investigatePointTimer = 0f;
                investigateTimer = 0f;

                agent.speed = patrolSpeed;
                agent.isStopped = false;

                SetLookingAround(false);

                if (patrolPoints != null && patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[currentPointIndex].position);
                break;

            case EnemyState.Check:
                isChasing = false;
                isInvestigating = false;

                _isWaitingAtInvestigatePoint = false;
                _investigatePointTimer = 0f;
                investigateTimer = 0f;

                agent.speed = patrolSpeed;
                agent.isStopped = false;

                SetLookingAround(false);

                agent.SetDestination(_lastSeenPosition);
                break;

            case EnemyState.Investigate:
                isChasing = false;
                isInvestigating = true;
                IsChecking = false;

                agent.speed = patrolSpeed;
                agent.isStopped = false;

                investigateTimer = 0f;
                _isWaitingAtInvestigatePoint = false;
                _investigatePointTimer = 0f;

                AudioManager.I.Play(investigateSounds, transform.position);
                SetLookingAround(false);

                // Сначала попробуем пойти к центру, чтобы выглядело естественно
                agent.SetDestination(_investigateCenter);
                break;

            case EnemyState.Chase:
                isChasing = true;
                isInvestigating = false;
                IsChecking = false;

                _isWaitingAtInvestigatePoint = false;
                _investigatePointTimer = 0f;
                investigateTimer = 0f;

                SetLookingAround(false);

                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;
        }
    }

    void AnimUpdate()
    {
        float speed01 = 0f;
        if (agent.speed > 0.001f)
            speed01 = agent.velocity.magnitude / agent.speed;

        animator.SetFloat(Speed, speed01, dampTime, Time.deltaTime);
    }

    void UpdateSuspicion(bool canSeePlayer, bool hearsNoise)
    {
        if (canSeePlayer)
        {
            SetLookingAround(false);
            float dist = Vector3.Distance(transform.position, player.position);

            float gainPerSec = GetVisionSuspicionGainPerSecond(dist);
            suspicion += gainPerSec * Time.deltaTime;

            _lastSeenPosition = player.position;
            _investigateCenter = _lastSeenPosition;
        }

        else if (hearsNoise && suspicion <= 0.49f)
        {
            SetLookingAround(false);
            suspicion = 0.5f;
            _lastHeardPosition = player.position;
        }

        else
        {
            suspicion -= suspicionDecreaseSpeed * Time.deltaTime;
        }


        if (IsChecking)
        {
            suspicion = Mathf.Max(suspicion, suspicionToСheck);
        }

        if (isInvestigating)
        {
            suspicion = Mathf.Max(suspicion, suspicionToInvestigate);
        }

        if (isChasing)
        {
            suspicion = Mathf.Max(suspicion, suspicionToChase);
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

        if (suspicion >= suspicionToСheck && suspicion < suspicionToInvestigate)
        {
            currentState = EnemyState.Check;
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
        IsChecking = false;
        agent.speed = patrolSpeed;

        investigateTimer += Time.deltaTime;

        if (investigateTimer >= investigateDuration)
        {
            SetLookingAround(false);

            isInvestigating = false;
            investigateTimer = 0f;
            _isWaitingAtInvestigatePoint = false;
            _investigatePointTimer = 0f;

            currentState = EnemyState.Patrol;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (CanSeePlayer())
            {
                SetLookingAround(false);

                _lastSeenPosition = player.position;
                _investigateCenter = _lastSeenPosition;

                _isWaitingAtInvestigatePoint = false;
                _investigatePointTimer = 0f;

                agent.isStopped = false;
                agent.SetDestination(_lastSeenPosition);
                return;
            }

            if (!_isWaitingAtInvestigatePoint)
            {
                _isWaitingAtInvestigatePoint = true;
                _investigatePointTimer = 0f;

                agent.isStopped = true;
                SetLookingAround(true);
            }

            _investigatePointTimer += Time.deltaTime;

            if (_investigatePointTimer >= investigatePointWaitTime)
            {
                _isWaitingAtInvestigatePoint = false;
                _investigatePointTimer = 0f;

                agent.isStopped = false;
                SetLookingAround(false);

                PickNewInvestigateDestination();
            }
        }
        else
        {
            agent.isStopped = false;
            SetLookingAround(false);
        }
    }

    void UpdateCheck()
    {
        IsChecking = true;
        isInvestigating = false;
        isChasing = false;
        agent.speed = patrolSpeed; // отдельную скорость?


        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            IsChecking = false;

            currentState = EnemyState.Patrol;
        }
    }

    private void PickNewInvestigateDestination()
    {

        for (int i = 0; i < 3; i++) // investigatePoints?
        {
            Vector2 rnd = Random.insideUnitCircle * investigateRadius;
            Vector3 candidate = _investigateCenter + new Vector3(rnd.x, 0f, rnd.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navSampleMaxDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
            
        }

        agent.SetDestination(_investigateCenter);
    }

    private void SetLookingAround(bool value)
    {
        if (animator != null)
            animator.SetBool(Lookbool, value);
    }

    /*void SetLookingAround() //todo: с анимацией
    {

        //transform.Rotate(Vector3.up, lookAroundSpeed * Time.deltaTime);
        SetLookingAround(true);

    }*/

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
        isChasing = true;
        isInvestigating = false;
        investigateTimer = 0f;
        IsChecking = false;
        chaseTimer += Time.deltaTime;

        if (CanSeePlayer())
        {
            SetLookingAround(false);
            chaseTimer = 0f;
            agent.speed = chaseSpeed;
            _lastSeenPosition = player.position;
            agent.SetDestination(player.position);

        }

        else if (chaseTimer >= chaseDuration) //todo: зафиксировать?
        {

            currentState = EnemyState.Investigate;
            agent.SetDestination(_lastSeenPosition);

        }
    }


    bool CanSeePlayer()
    {
        float viewRadius, viewAngle;
        GetVisionFromLight(out viewRadius, out viewAngle);

        Transform source = viewSource;

        //Transform source = viewSource != null ? viewSource : transform;

        Vector3 eyePosition = source.position;
        Vector3 forward = source.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 toPlayer = player.position - eyePosition;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > viewRadius)
            return false;

        Vector3 flatDirectionToPlayer = toPlayer;
        flatDirectionToPlayer.y = 0f;
        flatDirectionToPlayer.Normalize();

        float angleToPlayer = Vector3.Angle(forward, flatDirectionToPlayer);
        if (angleToPlayer > viewAngle * 0.5f)
            return false;

        Debug.DrawRay(eyePosition, flatDirectionToPlayer * distanceToPlayer, Color.red);

        if (Physics.Raycast(
                eyePosition,
                (player.position - eyePosition).normalized,
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
        //t = Mathf.Pow(t, lightGamma);

        viewRadius = Mathf.Lerp(viewRadiusDark, viewRadiusLight, t);
        viewAngle = Mathf.Lerp(viewAngleDark, viewAngleLight, t);
    }

    public void HearNoise(Vector3 noisePosition, float noiseRadius)
    {
        if (isDead) return;

        if (currentState == EnemyState.Chase)
            return;

        _lastHeardPosition = noisePosition;
        _investigateCenter = noisePosition;
        heardNoise = true;

        if (currentState == EnemyState.Investigate)
        {
            investigateTimer = 0f;
            _isWaitingAtInvestigatePoint = false;
            _investigatePointTimer = 0f;
            agent.isStopped = false;
            PickNewInvestigateDestination();
            return;
        }

        // Иначе переходим в Investigate — OnEnterState задаст новую точку
        investigateTimer = 0f;
        _isWaitingAtInvestigatePoint = false;
        _investigatePointTimer = 0f;
        agent.isStopped = false;

        currentState = EnemyState.Investigate;
    }

    public void OnBottleHit(HitZone zone, Vector3 hitPoint, Vector3 hitDir)
    {
        Debug.Log("OnBottleHit");
        if (isDead) return;

        if (zone == HitZone.Head)
        {
            Knockout(hitPoint, hitDir);
            return;
        }

        // Body/Legs — позже решишь (урон/стан/агр)
    }

//todo: переместить логику нокдауна в другой скрипт
    public void Knockout(Vector3 hitPoint, Vector3 hitDir)
    {
        if (ragdoll == null) ragdoll = GetComponent<EnemyRagdoll>();
        if (ragdoll != null)
            ragdoll.EnableRagdoll(hitPoint, hitDir);

        _knocked = true;
        OnEnemyKnocked?.Invoke();

        // опционально: через N секунд “поднять” (если нужно)
        // StartCoroutine(RecoverAfter(5f));
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDir)
    {
        if (isDead) return;

        hp -= amount;

        // if (animator != null)
        //animator.SetTrigger(HitTrig);

        if (hp <= 0f)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        OnEnemyDead?.Invoke();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null)
            animator.SetTrigger(DieTrig);

        Destroy(gameObject);
    }

    private float GetVisionSuspicionGainPerSecond(float distance)
    {
        // 0 у far, 1 у near
        float t = Mathf.InverseLerp(suspicionFarDist, suspicionNearDist, distance);
        t = Mathf.Clamp01(t);

        // можно сделать кривую (Dishonored любит не линейность)
        //if (distanceToGain != null)
        // t = Mathf.Clamp01(distanceToGain.Evaluate(t));

        return Mathf.Lerp(suspicionGainFar, suspicionGainNear, t);
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

    void OnDrawGizmosSelected()
    {
        float viewRadius, viewAngle;
        GetVisionFromLight(out viewRadius, out viewAngle);

        Transform source = viewSource;

        Vector3 origin = source.position;
        Vector3 forward = source.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion leftRot = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up);

        Vector3 leftBoundary = leftRot * forward;
        Vector3 rightBoundary = rightRot * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + leftBoundary * viewRadius);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewRadius);

        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(origin, player.position);
        }

        if (currentState == EnemyState.Investigate && CanSeePlayer())
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawSphere(_lastSeenPosition, 0.2f);
        }

        if (currentState == EnemyState.Investigate && heardNoise)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_lastHeardPosition, 0.2f);
        }
    }
}
