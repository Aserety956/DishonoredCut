using UnityEngine;
using UnityEngine.InputSystem;

public class Emit : MonoBehaviour
{
    public float walkNoiseRadius = 8f;
    public float runNoiseRadius = 12f;
    public float crouchNoiseMultiplier = 0.3f;
    
    public float noiseInterval = 0.3f;
    
    private float noiseTimer;

    private CharacterController controller;
    private PlayerController playerController;
    
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
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
        
        float noiseRadius = IsRunning(speed) ? runNoiseRadius : walkNoiseRadius;
        
        if (playerController.isCrouching)
        {
            noiseRadius *= crouchNoiseMultiplier;
        }

        EmitNoise(noiseRadius);

        noiseTimer = 0f;
    }
    
    bool IsRunning(float speed)
    {
        return speed > 5.1f;
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

        Gizmos.color = Color.pink;
        Gizmos.DrawWireSphere(transform.position, runNoiseRadius);
        
    }
}
