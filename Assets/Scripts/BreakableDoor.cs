using UnityEngine;

public class BreakableDoor : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHp = 50f;
    private float hp;

    [Header("Broken Prefab")]
    [SerializeField] private GameObject brokenDoorPrefab;
    public Transform brokenDoorTransform;

    [Header("Break Force")]
    [SerializeField] private float explosionForce = 6f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float upwardsModifier = 0.2f;

    [Header("Optional")]
    [SerializeField] private bool destroyIntactObject = true;

    private bool isBroken;

    private void Awake()
    {
        hp = maxHp;
    }

    
    // Нанести урон двери. hitPoint/hitDir нужны, чтобы красиво толкнуть куски.
    
    public void ApplyDamage(float damage, Vector3 hitPoint, Vector3 hitDir)
    {
        Debug.Log($"Break() called. brokenDoorPrefab={(brokenDoorPrefab ? brokenDoorPrefab.name : "NULL")}");
        if (isBroken) return;

        hp -= damage;
        if (hp > 0f) return;

        Break(hitPoint, hitDir);
    }

    private void Break(Vector3 hitPoint, Vector3 hitDir)
    {
        isBroken = true;
        
        
        GameObject broken = Instantiate(
            brokenDoorPrefab,
            transform.position,
            transform.rotation
        );
        
        // 2) Толкаем куски (эффект удара/взрыва) //todo: сделать более лукабельно
        Rigidbody[] rbs = broken.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < rbs.Length; i++)
        {
            // Небольшой "взрыв" от точки удара
            rbs[i].AddExplosionForce(explosionForce, hitPoint, explosionRadius, upwardsModifier, ForceMode.Impulse);

            // Доп. толчок в направлении удара (чтобы летело "от игрока")
            rbs[i].AddForce(hitDir * (explosionForce * 0.35f), ForceMode.Impulse);
        }
        
        if (destroyIntactObject)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
