using System;
using UnityEngine;

public  class BottleItem : MonoBehaviour
{
    [Header("Noise")]
    public float breakNoiseRadius  = 8f;

    [Header("Break visuals")]
    public GameObject fracturedPrefab;
    public float fracturedLifetime = 8f;
    public float fracturedExplosion = 2.5f;
    public float fracturedUp = 0.6f;

    private Rigidbody _rb;
    private Collider _col;

    public bool isHeld;

    // КЛЮЧ: пока armed=false — бутылка НИКОГДА не шумит и не ломается
    private bool _armed;

    //private float _nextImpactTime;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    // ---------- API для игрока ----------

    public void PickupTo(Transform holdPoint, Vector3 localPos, Vector3 localEuler)
    {
        isHeld = true;

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
        isHeld = false;

        transform.SetParent(null, worldPositionStays: true);
        transform.position = pos;
        transform.rotation = rot;

        _col.enabled = true;
    }

    // ---------- Physics: шум / разбитие ----------

    void OnCollisionEnter(Collision c)
    {
        if (isHeld) return;

        // КЛЮЧ: не armed => вообще ничего не делаем
        if (!_armed) return;

        //if (Time.time < _nextImpactTime) return;

        //float speed = c.relativeVelocity.magnitude;
        //if (speed < minImpactSpeed) return;

        //_nextImpactTime = Time.time + impactCooldown;
        
            if (breakNoiseRadius > 0f)
                NoiseEmmiter.EmitNoiseAt(transform.position, breakNoiseRadius);

            Break(c);
    }

    void Break(Collision c)
    {
        if (fracturedPrefab != null)
        {
            
            Vector3 spawnPos = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            Quaternion spawnRot = transform.rotation;
            
            var go = Instantiate(fracturedPrefab, spawnPos, spawnRot);

            var rbs = go.GetComponentsInChildren<Rigidbody>();

            Vector3 dir = Vector3.up;
            if (c.contactCount > 0)
                dir = -c.GetContact(0).normal;

            for (int i = 0; i < rbs.Length; i++)
            {
                rbs[i].AddForce((dir + Vector3.up * fracturedUp) * fracturedExplosion, ForceMode.VelocityChange);
            }

            if (fracturedLifetime > 0f)
                Destroy(go, fracturedLifetime);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, breakNoiseRadius);
    }
}
