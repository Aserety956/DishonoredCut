using UnityEngine;

public class LightSensor : MonoBehaviour
{
    public float lightPower = 1f;
        public LayerMask obstacleMask;
    
        private Light spotLight;
            //think!
        void Awake()
        {
            spotLight = GetComponent<Light>();
        }
    
        public float GetLightValue(Vector3 targetPosition)
        {
            Vector3 dir = targetPosition - transform.position;
            float distance = dir.magnitude;
            
            if (distance > spotLight.range)
                return 0f;
    
            float angle = Vector3.Angle(transform.forward, dir);
    
            if (angle > spotLight.spotAngle / 2f)
                return 0f;
    
            // Проверка на препятствия
            if (Physics.Raycast(
                transform.position,
                dir.normalized,
                distance,
                obstacleMask))
            {
                return 0f;
            }
    
            // Ослабление по дистанции
            float distanceFactor = 1f - (distance / spotLight.range);
    
            return distanceFactor * lightPower;
        }
}
