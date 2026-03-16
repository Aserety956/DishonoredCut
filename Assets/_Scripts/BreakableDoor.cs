using UnityEngine;

public class BreakableDoor : MonoBehaviour, IDamageable, IInteractable
{
    [Header("Health")]
    [SerializeField] private float maxHp = 50f;
    public float hp;

    [Header("Broken Prefab")]
    [SerializeField] private GameObject brokenDoorPrefab;

    [Header("Break Force")]
    [SerializeField] private float explosionForce = 6f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float upwardsModifier = 0.2f;

    [Header("Optional")]
    [SerializeField] private bool destroyIntactObject = true;
    
    [Header("Animation")]
    private Animator animator;
    private bool isOpen;
    
    [Header("Outline")]
    [SerializeField] private Behaviour outlineBehaviour; // встроить свой outline behavior

    private bool isBroken;

    private void Awake()
    {
        hp = maxHp;
        animator = GetComponent<Animator>();
        SetHighlight(false);
    }

    public void Interact(PlayerController player)
    {
        if (isBroken)
            return;
        
        Toggle();
    }

    public void SetHighlight(bool enabled)
    {
        if (outlineBehaviour != null)
            outlineBehaviour.enabled = enabled;
    }
    
    public void Toggle()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
    }
    
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDir)
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
        SetHighlight(false);
        
        
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
    
    public string GetInteractText()
    {
        if (isBroken)
            return string.Empty;

        return isOpen ? "E — Close door" : "E — Open door";
    }
}
