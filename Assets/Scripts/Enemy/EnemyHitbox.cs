using System;
using UnityEngine;


public enum HitZone { Head, Body, Legs }

public class EnemyHitbox : MonoBehaviour
{
    public HitZone zone;
    public EnemyController enemy;
    private BottleItem _bottle;

    public void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Bottle"))
            return;

        var hitBottle = other.GetComponent<BottleItem>();
        if (hitBottle == null) 
            return;

        if (hitBottle._isHeld) return;
        //if (!bottle._armed) return;
        if (hitBottle._broken) return;
        if (hitBottle._hitEnemy) return;

        var hitbox = other.GetComponent<BottleItem>();
        if (hitbox == null) return;
        Debug.Log($"[HEAD HITBOX] Trigger enter by {other.name} (layer {other.gameObject})");

        hitBottle._hitEnemy = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDir = (hitBottle._rb.linearVelocity.sqrMagnitude > 0.0001f) ? hitBottle._rb.linearVelocity.normalized : transform.forward;

        enemy.OnBottleHit(zone, hitPoint, hitDir);

        // обычно бутылка разбивается при попадании
        hitBottle.BreakInternal(hitPoint, hitDir);
    }
}
