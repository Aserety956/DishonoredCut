using UnityEngine;
using UnityEngine.InputSystem;

public class NoiseEmmiter : MonoBehaviour
{
   [Header("Footsteps (player only)")]
    public bool enableFootstepNoise = true;

    public float walkNoiseRadius = 8f;
    public float runNoiseRadius = 12f;
    public float crouchNoiseMultiplier = 0.3f;
    public float noiseInterval = 0.3f;

    private float noiseTimer;

    private CharacterController controller;
    private PlayerController playerController;

    // --- shared (for items + player) ---
    static int enemyMask;
    static readonly Collider[] overlapBuffer = new Collider[8];

    void Awake()
    {
        if (enemyMask == 0)
            enemyMask = LayerMask.GetMask("Enemy");
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!enableFootstepNoise)
            return;

        if (controller == null || playerController == null)
            return;

        noiseTimer += Time.deltaTime;

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed < 0.1f)
            return;

        if (noiseTimer < noiseInterval)
            return;

        float noiseRadius = IsRunning(speed) ? runNoiseRadius : walkNoiseRadius;

        if (playerController.isCrouching)
            noiseRadius *= crouchNoiseMultiplier;

        EmitNoiseAt(transform.position, noiseRadius);

        noiseTimer = 0f;
    }

    bool IsRunning(float speed) => speed > 5.1f;

    /// <summary>
    /// Универсальный шум: игрок, бутылка, любой предмет.
    /// radius = "сила" шума (у тебя враги уже принимают radius).
    /// </summary>
    public static void EmitNoiseAt(Vector3 position, float radius)
    {
        if (radius <= 0f) return;

        int count = Physics.OverlapSphereNonAlloc(position, radius, overlapBuffer, enemyMask);

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuffer[i];
            if (col == null) continue;

            var enemy = col.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.HearNoise(position, radius);

            overlapBuffer[i] = null; // очищаем, чтобы не держать ссылки
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
