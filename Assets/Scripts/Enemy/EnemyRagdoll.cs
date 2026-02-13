using UnityEngine;
using UnityEngine.AI;

public class EnemyRagdoll : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private MonoBehaviour aiScript;   
    [SerializeField] private Collider rootCollider; 
    [SerializeField] private Collider headCollider;
    [SerializeField] private Collider bodyCollider; 

    [Header("Ragdoll parts (bones)")]
    [SerializeField] private Rigidbody[] bodies;
    [SerializeField] private Collider[] colliders;

    private bool _isRagdoll;

    private void Awake()
    {
        if (agent == null) agent = GetComponentInChildren<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rootCollider == null) rootCollider = GetComponent<Collider>();

        // если не задал в инспекторе — соберём автоматически
        if (bodies == null || bodies.Length == 0)
            bodies = GetComponentsInChildren<Rigidbody>(true);
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);

        SetRagdoll(false);
    }

    public void EnableRagdoll(Vector3 hitPoint, Vector3 hitDir, float impulse = 6f)
    {
        if (_isRagdoll) return;
        SetRagdoll(true);

        // Импульс в ближайшую кость (для “эффекта падения”)
        var rb = FindClosestBody(hitPoint);
        if (rb != null)
            rb.AddForceAtPosition(hitDir.normalized * impulse, hitPoint, ForceMode.Impulse);
    }

    private void SetRagdoll(bool enabled)
    {
        _isRagdoll = enabled;

        // Выключаем управление
        if (agent != null) agent.enabled = !enabled;
        if (aiScript != null) aiScript.enabled = !enabled;
        if (animator != null) animator.enabled = !enabled;

        // Root капсула обычно мешает ragdoll — выключаем
        if (rootCollider != null) rootCollider.enabled = !enabled;

        // Включаем физику на костях
        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            if (rb == null) continue;

            // Не трогаем rigidbody на root, если он есть (часто его нет)
            if (rb.gameObject == gameObject) continue;

            /*rb.isKinematic = !enabled;
            rb.detectCollisions = enabled;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;*/
        }

        // Коллайдеры костей: включены только в ragdoll
        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];
            if (col == null) continue;
            if (col == rootCollider) continue;
            if (col == headCollider) continue;
            if (col == bodyCollider)  continue;

            col.enabled = enabled;
        }
    }

    private Rigidbody FindClosestBody(Vector3 p)
    {
        Rigidbody best = null;
        float bestD = float.PositiveInfinity;

        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            if (rb == null) continue;
            if (rb.gameObject == gameObject) continue;

            float d = (rb.worldCenterOfMass - p).sqrMagnitude;
            if (d < bestD) { bestD = d; best = rb; }
        }
        return best;
    }
}
