using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BottleItem : MonoBehaviour, IDamageable
{
    [Header("Noise")]
    public float breakNoiseRadius  = 8f;

    [Header("Break visuals")]
    public GameObject fracturedPrefab;
    public float fracturedLifetime = 5f;
    public float fracturedExplosion = 2.5f;
    public float fracturedUp = 0.6f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] breakClip;
    [SerializeField, Range(0f, 1f)] private float breakVolume = 1f;

    private Rigidbody _rb;
    private Collider _col;
    [SerializeField] private CharacterController playerController;

    private bool _isHeld;

    // КЛЮЧ: пока armed=false — бутылка НИКОГДА не шумит и не ломается
    private bool _armed;
    
    private bool _broken;
    

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

    /// <summary>
    /// Если игрок ударил бутылку (мечом/кулаком) — активируем.
    /// Зови это из своей системы атаки при попадании по бутылке.
    /// </summary>
    public void ArmByHit()
    {
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
    
    private void BreakInternal(Vector3 hitPoint, Vector3 hitDir)
    {
        if (_broken) return;
        _broken = true;

        if (_col != null) _col.enabled = false;
        
        if (breakClip != null && breakClip.Length > 0)
        {
            int index = Random.Range(0, breakClip.Length);
            AudioSource.PlayClipAtPoint(breakClip[index], transform.position, breakVolume);
        }
        
        if (breakNoiseRadius > 0f)
            NoiseEmmiter.EmitNoiseAt(transform.position, breakNoiseRadius);
        
        if (fracturedPrefab != null)
        {
            Quaternion spawnRot = transform.rotation;
            var go = Instantiate(fracturedPrefab, hitPoint, spawnRot);

            var rbs = go.GetComponentsInChildren<Rigidbody>();
            Vector3 dir = (hitDir.sqrMagnitude > 0.0001f) ? hitDir.normalized : Vector3.up;

            for (int i = 0; i < rbs.Length; i++)
                rbs[i].AddForce((dir + Vector3.up * fracturedUp) * fracturedExplosion, ForceMode.VelocityChange);

            if (fracturedLifetime > 0f)
                Destroy(go, fracturedLifetime);
        }

        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, breakNoiseRadius);
    }
}
