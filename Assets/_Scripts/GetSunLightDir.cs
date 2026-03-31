using UnityEngine;

[ExecuteInEditMode]
public class GetSunLightDir : MonoBehaviour
{
    [SerializeField] private Material skyboxMat;
    
    private void Update()
    {
        skyboxMat.SetVector("_MainLightDirection", transform.forward);
    }
}
