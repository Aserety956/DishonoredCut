using System;
using UnityEngine;

public class LightSensor : MonoBehaviour
{
    public float lightPower = 1f;
    public LayerMask obstacleMask;
    
    private Light spotLight;
            //think!
    void Awake()
    { spotLight = GetComponent<Light>();
    }
    
    public float GetLightValue(Vector3 targetPosition)
    {
        Vector3 dirToTarget = targetPosition - transform.position; //вектор направления (конец - начало)
        float distance3D = dirToTarget.magnitude; // длина вектора √(x^2 + y^2 + z^2)= √c = ... sqrMagnitude 
        
        if (distance3D > spotLight.range) return 0f;
    
        float angle = Vector3.Angle(transform.forward, dirToTarget.normalized); //деление на длину
        if (angle > spotLight.spotAngle / 2f)
                return 0f;
    
        Vector3 dirToTargetXZ = dirToTarget;
        dirToTargetXZ.y = 0f;
        
        float distanceXZ = dirToTargetXZ.magnitude; //2Dvector
        
        Debug.DrawRay(transform.position, dirToTarget, Color.yellow);
        
        if (Physics.Raycast(
                transform.position, dirToTarget.normalized, distance3D, obstacleMask))
        { 
                return 0f;
        }
    
        // Ослабление по дистанции
        float distanceFactor = 1f - (distanceXZ / spotLight.range);
    
        return Mathf.Clamp01(distanceFactor * lightPower);
    }
    
}
