using System;
using System.Xml.XPath;
using UnityEditor;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform[] patrolPoints;

    public Vector3 enemyPos;

    public float moveSpeed = 1.5f;
    
    private int currentPointIndex = 0;
    
    private float lerpT = 0f;

    public bool start;

    public float timePatrol;


    private void Start()
    {
        start = true;
    }

    void Update()
    {
        if (start)
        {
            timePatrol += Time.deltaTime;
        }
        
        if (patrolPoints.Length == 0)
            return;
        
        Vector3 target = patrolPoints[currentPointIndex].position;
        
        target.y = transform.position.y;
        
        lerpT = Time.deltaTime * moveSpeed;

        transform.position = Vector3.Lerp(transform.position, target, lerpT);

        /*if (lerpT >= 1f)
        {
            currentPointIndex++;

            if (currentPointIndex >= patrolPoints.Length)
            {
                currentPointIndex = 0;

            }
        }*/

        if (timePatrol >= 5)
        {
            currentPointIndex++;
            timePatrol = 0;
            
            if (currentPointIndex >= patrolPoints.Length)
            {
                currentPointIndex = 0;

            }
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
}
