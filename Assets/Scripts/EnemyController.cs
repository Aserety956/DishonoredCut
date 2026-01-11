using System;
using System.Xml.XPath;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    
    [Header("Investigation")]
    [SerializeField] private float investigateWaitTime = 3f;
    [SerializeField] private float lookAroundSpeed = 120f;
    private float investigateTimer;
    private bool isLookingAround;
    
    [Header("FOV")]
    public float viewRadius = 8f;      
    public float viewAngle = 90f;      
    public LayerMask targetMask;       
    public LayerMask obstacleMask;    
    
    public Transform[] patrolPoints;
    public Transform player;
    
    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float waitTimeAtPoint = 2f;
    
    
    [Header("Chase")]
    public float chaseSpeed = 4f;
    
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

        switch (currentState)
        {
           case EnemyState.Patrol:
               UpdatePatrol();
               CheckPlayerDetection();
               break;
           
           case EnemyState.Investigate:
               Investigate();
               break;
           
           case EnemyState.Chase:
               UpdateChase();
               break;
        }
        
    }
    
    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;

        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }
    
    void CheckPlayerDetection()
    {
        if (CanSeePlayer())
        {
            currentState = EnemyState.Chase;
        }
    }
    
    void Investigate()
    {
        if (CanSeePlayer())
        {
            currentState = EnemyState.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;

            LookAround();

            investigateTimer += Time.deltaTime;

            if (investigateTimer >= investigateWaitTime)
            {
                
                agent.isStopped = false;
                investigateTimer = 0f;
                isLookingAround = false;

                currentState = EnemyState.Patrol;
                GoToNextPoint();
            }
        }
    }
    void LookAround()
    {
        isLookingAround = true;
        
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
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
        
        if (!CanSeePlayer())
        {
            currentState = EnemyState.Patrol;
            GoToNextPoint();
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
    
    private Vector3 lastHeardPosition;
    public void HearNoise(Vector3 noisePosition, float noiseRadius)
    {
        
        if (currentState == EnemyState.Chase)
            return;
        
        
        lastHeardPosition = noisePosition;
        
        investigateTimer = 0f;
        isLookingAround = false;
        agent.isStopped = false;
        
        agent.SetDestination(lastHeardPosition);
        
        currentState = EnemyState.Investigate;
        
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        
        
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
            Gizmos.DrawSphere(lastHeardPosition, 0.2f);
        }
        
    }
    
}
