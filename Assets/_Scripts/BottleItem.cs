using System;
using UnityEngine;

public class BottleItem : MonoBehaviour, IDamageable, IInteractable
{
    [Header("Noise")]
    public float breakNoiseRadius  = 8f;

    [Header("Break prefab")]
    public GameObject fracturedPrefab;
    public float fracturedLifetime = 5f;
    public float fracturedExplosion = 2.5f;
    public float fracturedUp = 0.6f;
    
    [Header("VFX")]
    [SerializeField] private ParticleSystem breakVfxPrefab; 
    [SerializeField] private float breakVfxLifetime = 3f;    
    [SerializeField] private bool vfxSpawnAtHitPoint = true; // в точке удара или в центре бутылки
    
    [Header("Audio")]
    [SerializeField] private SoundData crackSound;
    
    [Header("Throw Rotation")]
    [SerializeField] private float minSpin = 10f;
    [SerializeField] private float maxSpin = 18f;
    [SerializeField] private float forwardSpinFactor = 0.35f;
    [SerializeField] private float twistSpinFactor = 0.2f;
    
    [Header("Highlight")]
    [SerializeField] private Behaviour outlineBehaviour;

    public Rigidbody _rb;
    public Collider _col;
    [SerializeField] private CharacterController playerController;

    public bool _isHeld;

    // КЛЮЧ: пока armed=false — бутылка НИКОГДА не шумит и не ломается
    public bool _armed;
    
    public bool _broken;
    
    public bool _hitEnemy;
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        
        if (playerController != null)
            Physics.IgnoreCollision(_col, playerController);
        
        SetHighlight(false);
    }

    // ---------- API для игрока ----------
    
    public void Interact(PlayerController player)
    {
        if (player == null) return;
        if (_broken) return;
        if (_isHeld) return;

        player.PickupItem(this);
    }
    
    public void PickupTo(Transform holdPoint, Vector3 localPos, Vector3 localEuler)
    {
        _isHeld = true;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;

        _col.enabled = false;

        transform.SetParent(holdPoint, worldPositionStays: false);
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.Euler(localEuler);
        
        _armed = false;
    }
    
    
    public void ReleaseDrop(Vector3 pos, Quaternion rot)
    {
        DetachToWorld(pos, rot);

        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _armed = false; // важно
    }
    
    public void ThrowFrom(Vector3 origin, Quaternion rotation, Vector3 velocityChange)
    {
        DetachToWorld(origin, rotation);

        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.AddForce(velocityChange, ForceMode.VelocityChange);
        
        float mainSpin = UnityEngine.Random.Range(minSpin, maxSpin);
        float sideSign = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        float forwardSign = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        float twistSign = UnityEngine.Random.value > 0.5f ? 1f : -1f;

        Vector3 angularVelocity =
            transform.right * (mainSpin * sideSign) +
            transform.forward * (mainSpin * forwardSpinFactor * forwardSign) +
            transform.up * (mainSpin * twistSpinFactor * twistSign);

        _rb.angularVelocity = angularVelocity;

        _armed = true;
    }
    

    void DetachToWorld(Vector3 pos, Quaternion rot)
    {
        _isHeld = false;

        transform.SetParent(null, worldPositionStays: true);
        transform.position = pos;
        transform.rotation = rot;

        _col.enabled = true;
    }

    // ---------- Physics: шум / разбитие ----------

    void OnCollisionEnter(Collision c)
    {
        if (_isHeld) return;
        if (!_armed) return; 
        
        BreakFromCollision(c);
    }
    
    private void BreakFromCollision(Collision c)
    {
        Vector3 hitPoint = (c.contactCount > 0) ? c.GetContact(0).point : transform.position;
        Vector3 hitDir   = (c.contactCount > 0) ? -c.GetContact(0).normal : Vector3.up;

        BreakInternal(hitPoint, hitDir);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDir)
    {
        BreakInternal(hitPoint, hitDir);
    }
    
    public void BreakInternal(Vector3 hitPoint, Vector3 hitDir)
    {
        SetHighlight(false);
        if (_broken) return;
        _broken = true;

        if (_col != null) _col.enabled = false;
        
        AudioManager.I.Play(crackSound, transform.position);
        
        if (breakNoiseRadius > 0f)
            NoiseEmmiter.EmitNoiseAt(transform.position, breakNoiseRadius);
        
        
        if (breakVfxPrefab != null)
        {
            Vector3 vfxPos = vfxSpawnAtHitPoint ? hitPoint : transform.position;
            Quaternion vfxRot = Quaternion.LookRotation(
                (hitDir.sqrMagnitude > 0.0001f) ? hitDir.normalized : transform.forward
            );

            var vfx = Instantiate(breakVfxPrefab, vfxPos, vfxRot);
            vfx.Play();

            // Удаляем объект с частицами после проигрыша
            if (breakVfxLifetime > 0f)
                Destroy(vfx.gameObject, breakVfxLifetime);
            else
                Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax + 0.2f);
        }

        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, breakNoiseRadius);
    }
    
    public void SetHighlight(bool enabled)
    {
        if (outlineBehaviour != null)
            outlineBehaviour.enabled = enabled;
    }
    
    public string GetInteractText()
    {
        if (_broken || _isHeld)
            return string.Empty;

        return "E — Pick up bottle";
    }
}
