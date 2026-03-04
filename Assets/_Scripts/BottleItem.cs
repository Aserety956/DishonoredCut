using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BottleItem : MonoBehaviour, IDamageable
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
        
        
    }

    // ---------- API для игрока ----------

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
        if (_broken) return;
        _broken = true;

        if (_col != null) _col.enabled = false;
        
        AudioManager.I.Play(crackSound, transform.position);
        
        /*if (breakClip != null && breakClip.Length > 0)
        {
            int index = Random.Range(0, breakClip.Length);
            AudioSource.PlayClipAtPoint(breakClip[index], transform.position, breakVolume);
        }*/
        
        if (breakNoiseRadius > 0f)
            NoiseEmmiter.EmitNoiseAt(transform.position, breakNoiseRadius);
        
        /*if (fracturedPrefab != null)
        {
            Quaternion spawnRot = transform.rotation;
            var go = Instantiate(fracturedPrefab, hitPoint, spawnRot);

            var rbs = go.GetComponentsInChildren<Rigidbody>();
            Vector3 dir = (hitDir.sqrMagnitude > 0.0001f) ? hitDir.normalized : Vector3.up;

            for (int i = 0; i < rbs.Length; i++)
                rbs[i].AddForce((dir + Vector3.up * fracturedUp) * fracturedExplosion, ForceMode.VelocityChange);

            if (fracturedLifetime > 0f)
                Destroy(go, fracturedLifetime);
        }*/
        
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
    
    /*private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HEAD HITBOX] Trigger enter by {other.name} (layer {other.gameObject})");
        if (_isHeld) return;
        if (!_armed) return;
        if (_broken) return;
        if (_hitEnemy) return;
        
        var hitbox = other.GetComponent<EnemyHitbox>();
        if (hitbox == null || hitbox.enemy == null) return;

        _hitEnemy = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDir = (_rb.linearVelocity.sqrMagnitude > 0.0001f) ? _rb.linearVelocity.normalized : transform.forward;

        hitbox.enemy.OnBottleHit(hitbox.zone, hitPoint, hitDir);

        // обычно бутылка разбивается при попадании
        BreakInternal(hitPoint, hitDir);
    }*/
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, breakNoiseRadius);
    }
}
