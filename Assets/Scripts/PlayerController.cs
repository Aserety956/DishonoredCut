using System;
using System.Collections;
using System.Text.RegularExpressions;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
// private (_doSomething) // public (DoSomething) // static s(_Public, _private)
// interface (IDoSomething)
    [Header("Movement")]
    public float currentSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float gravity;
    public bool isCrouching;
    public bool isSwordEquipped;
    
    [Header("Interact")]
    public float interactDistance = 3f;
    public LayerMask interactMask;
    public LayerMask itemMask;
    public float throwForce; // на будущее: сила зависит от навыка
    
    [Header("Melee")]
    [SerializeField] private float meleeRange = 2.0f;
    [SerializeField] private float meleeRadius = 0.25f;
    [SerializeField] private float meleeDamage = 25f;
    [SerializeField] private float behindAngle = 140f;     // 140-160"со спины" todo:почекать
    [SerializeField] private LayerMask meleeMask;          // enemy and breakable stuff
    private float _attackStartTime;
    private float _attackHitTime;
    private float _attackEndTime;
    private bool _attackHitDone;
    [SerializeField] private float attackHitDelay;
    [SerializeField] private float attackTotalTime;
    public bool _isAttacking;
    
    [Header("Animation")] 
    [SerializeField] private Animator animator;
    private static readonly int AttackTrig = Animator.StringToHash("Attack");
    
    
    [Header("Vignette")]
    [SerializeField] private VolumeProfile volumeProfile;
    public float vignetteSmoothTime = 0.12f;
    private float _vignetteIntensity;
    
    [Header("CrouchLogic")]
    [SerializeField] private Transform headTarget;
    private float _cameraYVelocity;
    public float crouchSmoothTime = 0.12f;
    public float crouchHeadOffset = -0.5f;
    
    
    [Header("UI")]
    [SerializeField] private Image fillImageMana;
    [SerializeField] private Image fillImageHealth;
    [SerializeField] private Image backgroundImage;

    [Header("Stats")] 
    public static float HP;
    public static float MP;
    public static float currentHP;
    public static float currentMP;
    public static float baseHP;
    public static float baseMP;
    
    
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera cinCam;
    private BottleItem _heldItem;
    [SerializeField] private Transform handsPos;
    [SerializeField] private Transform itemPos;
    
    [Header("Debug")]
    [SerializeField] private GameObject itemPrefab;
    private Vector3 offsetToSpawn;
    
    
    private Vector3 _headInitialLocalPos;
    private float _headYVelocity;
    
    private Vector2 _move;
    private Vector3 _velocity;
    
    public void Start()
    {
        baseHP = 100f;
        baseMP = 100f;
        currentHP = baseHP;
        currentMP = baseMP;
        
        currentSpeed = walkSpeed;
        _velocity = Vector3.zero;
        _headInitialLocalPos = headTarget.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        HealthUpdate();
        ManaUpdate();
        
        float Fr = 1, To = 10, I=2;
        float t = Mathf.InverseLerp(Fr, To, I);

        Debug.Log(Mathf.InverseLerp(Fr, To, I)); // 1/9 = 0.11 (часть пути) t = (2 - 1) / (10 - 1) = 1/9 (c-a)/(b-a)
        Debug.Log(Mathf.Lerp(Fr, To, t)); // a + (b - a) * t или же 1+(9*1.9) = 2 (значение пути)
    }
    
    public void Update()
    {

        UpdateMove();

        UpdateAttack();
    }
    
    public void LateUpdate()
    {
        
        UpdateCam();
        
    }
    
    public void UpdateMove()
    {
        if (characterController.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        
        _velocity.y += gravity * Time.deltaTime;
        
        Vector3 move = ((GetForward() * _move.y + GetRight() * _move.x) * currentSpeed);
        
        characterController.Move((move + _velocity) * Time.deltaTime);
        
    }

    public void UpdateCam()
    {
        
        float targetOffset = isCrouching ? crouchHeadOffset : 0f;

        Vector3 localPos = headTarget.localPosition;
        localPos.y = Mathf.SmoothDamp(
            localPos.y,
            _headInitialLocalPos.y + targetOffset,
            ref _headYVelocity,
            crouchSmoothTime
        );

        headTarget.localPosition = localPos;
        
        volumeProfile.TryGet(out Vignette vignette);
        vignette.intensity.value = Mathf.SmoothDamp(
            vignette.intensity.value,
            isCrouching ? 0.25f : 0f,
            ref _vignetteIntensity,
            vignetteSmoothTime);
    }

    public void OnMove(InputValue val)
    { 
        _move = val.Get<Vector2>();
    }
    
    public void OnSprint(InputValue val)
    {
        if (val.Get<float>() > 0.5f)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }
    
    public void OnJump(InputValue val)
    {
        if (val.Get<float>() > 0.5f && characterController.isGrounded)
        {
            _velocity.y = 3f; 
        }
    }
    
    public void OnCrouch(InputValue val)
    {
        if (val.Get<float>() > 0.5f)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
    }

    public void OnDebug(InputValue val)
    {
        if (val.Get<float>() > 0.5f)
        {
            offsetToSpawn = new Vector3(1, 1, 1);
            Instantiate(itemPrefab, transform.position + offsetToSpawn, transform.rotation);
        }
    }

    private Vector3 GetForward()
    {
        Vector3 forward = cinCam.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }
    
    private Vector3 GetRight()
    {
        Vector3 right = cinCam.transform.right;
        right.y = 0;
        return right.normalized;
    }
    
    public void OnInteract(InputValue val)
    {
        if (val.Get<float>() > 0.5f)
        {
            Debug.Log("Interact"+ val.Get<float>());
            TryInteract();
            
        }
    }

    public void OnAttack(InputValue val)
    {
        if (!val.isPressed) return;
        
        if (_heldItem == null)
            TryAttack();
            
        else
        {
            Vector3 velocityChange = cinCam.transform.forward * throwForce;
            _heldItem.ThrowFrom(itemPos.position,itemPos.rotation,velocityChange);
            handsPos.localPosition += Vector3.up;
        }

    }

    public void HealthUpdate()
    {
        currentHP = Mathf.Clamp(currentHP,0,baseHP);
        fillImageHealth.fillAmount = currentHP/baseHP;
    }

    public void ManaUpdate()
    {
        currentMP = Mathf.Clamp(currentMP,0,baseMP);
        fillImageMana.fillAmount = currentMP/baseMP;
    }
    
    private void TryAttack()
    {
        if (_isAttacking) 
            return;

        _isAttacking = true;
        _attackHitDone = false;

        float now = Time.time;
        _attackStartTime = now;
        _attackHitTime   = now + attackHitDelay;
        _attackEndTime   = now + attackTotalTime;

        animator.SetTrigger(AttackTrig);
    }
    
    private void UpdateAttack()
    {
        if (!_isAttacking) return;

        float now = Time.time;

        // момент попадания
        if (!_attackHitDone && now >= _attackHitTime)
        {
            _attackHitDone = true;
            DoMeleeHit();
        }
        
        if (now >= _attackEndTime)
        {
            //float now = Time.time;

            _isAttacking = false;
        }
    }
    
    private void DoMeleeHit()
    {
        Ray ray = new Ray(cinCam.transform.position, cinCam.transform.forward);

        if (!Physics.SphereCast(ray, meleeRadius, out RaycastHit hit, meleeRange, meleeMask, QueryTriggerInteraction.Ignore))
            return;
        
        var enemy = hit.collider.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            if (IsBehindEnemy(enemy.transform))
                enemy.Die();
            else
                enemy.TakeDamage(meleeDamage, hit.point, ray.direction);

            return;
        }
        
        var damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(meleeDamage, hit.point, ray.direction);
            return;
        }
    }
    
    private bool IsBehindEnemy(Transform enemyRoot)
    {
        Vector3 enemyForward = enemyRoot.forward;
        enemyForward.y = 0f;
        enemyForward.Normalize();
        
        Vector3 enemyToPlayer = transform.position - enemyRoot.position;
        enemyToPlayer.y = 0f;

        if (enemyToPlayer.sqrMagnitude < 0.0001f)
            return false;

        enemyToPlayer.Normalize();

        // Если игрок сзади, угол близок к 180
        float angle = Vector3.Angle(enemyForward, enemyToPlayer);
        return angle > behindAngle;
    }

    private void TryInteract()
    {
        
        if (_heldItem != null)
        {
            _heldItem.ReleaseDrop(itemPos.position, itemPos.rotation);
            handsPos.localPosition += Vector3.up;
            _heldItem = null;
        }
        
        
        Ray ray = new Ray(cinCam.transform.position, cinCam.transform.forward);

        
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            BreakableDoor door = hit.collider.GetComponentInParent<BreakableDoor>();
            if (door != null)
            {
                door.Toggle();
            }
            
            
            BottleItem item = hit.collider.GetComponentInParent<BottleItem>(); 
            if (item != null)
            {
                handsPos.localPosition -= Vector3.up;
                _heldItem = item;
                _heldItem.PickupTo(itemPos, Vector3.zero, Vector3.zero);
                return;
            }
        }
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow, 0.2f);
            
        
    }
    
}
