using System;
using UnityEngine;


public enum HitZone { Head, Body, Legs }

public class EnemyHitbox : MonoBehaviour
{
    public HitZone zone;
    public EnemyController enemy;
    public BottleItem bottle;

    public void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Bottle"))
        {
            if (bottle._isHeld) return;
            //if (!bottle._armed) return;
            if (bottle._broken) return;
            if (bottle._hitEnemy) return;
            //Debug.Log($"[HEAD HITBOX] Trigger enter by {other.name} (layer {other.gameObject})");

            var hitbox = other.GetComponent<BottleItem>();
            if (hitbox == null) return;
            Debug.Log($"[HEAD HITBOX] Trigger enter by {other.name} (layer {other.gameObject})");

            bottle._hitEnemy = true;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDir = (bottle._rb.linearVelocity.sqrMagnitude > 0.0001f) ? bottle._rb.linearVelocity.normalized : transform.forward;

            enemy.OnBottleHit(zone, hitPoint, hitDir);

            // обычно бутылка разбивается при попадании
            bottle.BreakInternal(hitPoint, hitDir);
        }
    }
}
