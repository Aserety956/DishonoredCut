using System.Collections.Generic;
using UnityEngine;

public class LightDetector : MonoBehaviour
{
    private List<LightSensor> activeLights = new();
    public float currentLightLevel;

    void Update()
    {
        currentLightLevel = 0f;

        foreach (var light in activeLights)
        {
            currentLightLevel += light.GetLightValue(transform.position);
        }

        currentLightLevel = Mathf.Clamp01(currentLightLevel);
    }

    void OnTriggerEnter(Collider other)
    {
        LightSensor sensor = other.GetComponent<LightSensor>();
            activeLights.Add(sensor);
    }

    void OnTriggerExit(Collider other)
    {
        LightSensor sensor = other.GetComponent<LightSensor>();
            activeLights.Remove(sensor);
    }
}
