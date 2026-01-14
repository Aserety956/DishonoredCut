using System;
using System.Xml.XPath;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Suspicion")]
    public float suspicionIncreaseSpeed = 0.5f;
    public float suspicionDecreaseSpeed = 0.03f;

    public  float suspicionToInvestigate = 0.5f;
    public  float suspicionToChase = 1f;

    public float suspicion;
    
    private bool heardNoise;
    
    
    [Header("Investigation")]
    [SerializeField] float investigateDuration = 3f;
    [SerializeField] private float lookAroundSpeed = 120f;
    public float investigateTimer;
    public bool isInvestigating;
    
    private Vector3 lastHeardPosition;
    private Vector3 lastSeenPosition;
    
    [Header("FOV")]
    public float viewRadius = 8f;      
    public float viewAngle = 90f;      
    public LayerMask targetMask;       
    public LayerMask obstacleMask;    
    
    public Transform[] patrolPoints;
    public Transform player;
    
    [Header("Patrol")]
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


    private void Start()
    {

        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    void Update()
    {
        bool canSeePlayer = CanSeePlayer();
        
        UpdateSuspicion(canSeePlayer, heardNoise);
        EvaluateSuspicion();
        

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
    
    void UpdateSuspicion(bool canSeePlayer, bool hearsNoise)
    {
        if (canSeePlayer)
        {
            suspicion += suspicionIncreaseSpeed * Time.deltaTime;
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
        agent.speed = patrolSpeed;
        
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            
            agent.isStopped = true;

            LookAround();

            investigateTimer += Time.deltaTime;

            if (investigateTimer >= investigateDuration)
            {
                isInvestigating = false;
                
                agent.isStopped = false;
                investigateTimer = 0f;
                
                currentState = EnemyState.Patrol;
            }
        }
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
        
        //lostSightTimer = 0f; todo:доделать
        if (CanSeePlayer())
        { 
            agent.speed = chaseSpeed;
            lastSeenPosition = player.position;
            agent.SetDestination(player.position);
        }
        else
        // зафиксировать
        {
            
                currentState = EnemyState.Investigate;
                agent.SetDestination(lastSeenPosition);
            
        }
    }
    

    bool CanSeePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > viewRadius)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > viewAngle / 2)
            return false;

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
    
    
    public void HearNoise(Vector3 noisePosition, float noiseRadius)
    {
        
        if (currentState == EnemyState.Chase)
            return;
        
        
        lastHeardPosition = noisePosition;
        heardNoise = true;
        
        investigateTimer = 0f;
        agent.isStopped = false;

        if (currentState == EnemyState.Investigate  )
        {
            agent.SetDestination(lastHeardPosition);
        }
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
