using UnityEngine;
using UnityEngine.InputSystem;

public class Emit : MonoBehaviour
{
    public float walkNoiseRadius = 4f;
    public float runNoiseRadius = 8f;
    public float crouchRadius = 2f;
    
    public float noiseInterval = 0.4f;
    
    private float noiseTimer;

    private CharacterController controller;
    
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        noiseTimer += Time.deltaTime;
        
        Vector3 horizontalVelocity = new Vector3(
            controller.velocity.x,
            0,
            controller.velocity.z
        );

        float speed = horizontalVelocity.magnitude;
        
        if (speed < 0.1f)
            return;

        if (noiseTimer < noiseInterval)
            return;
        
        if (IsRunning())
        {
            EmitNoise(runNoiseRadius);
        }
        else
        {
            EmitNoise(walkNoiseRadius);
        }

        if (IsCrouching())
        {
            EmitNoise(crouchRadius);
        }

        noiseTimer = 0f;
    }
    
    bool IsRunning()
    {
        return Keyboard.current.leftShiftKey.isPressed;
    }
    
    bool IsCrouching()
    {
        return Keyboard.current.cKey.isPressed;
    }
    
    void EmitNoise(float radius)
    {
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            radius,
            LayerMask.GetMask("Enemy")
        );

        foreach (var col in enemies)
        {
            col.GetComponent<EnemyController>()?
                .HearNoise(transform.position, radius);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, walkNoiseRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, runNoiseRadius);
    }
}
