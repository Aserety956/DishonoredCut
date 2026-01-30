using System;
using System.Text.RegularExpressions;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{

    [Header("Movement")]
    public float currentSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float gravity;
    public bool isCrouching;
    
    
    [Header("Interact")]
    public float interactDistance = 3f;
    public LayerMask interactMask;
    
    [Header("Melee")]
    [SerializeField] private float meleeRange = 2.0f;
    [SerializeField] private float meleeRadius = 0.25f;
    [SerializeField] private float meleeDamage = 25f;
    [SerializeField] private float behindAngle = 140f;     // 140-160"со спины" todo:почекать
    [SerializeField] private LayerMask meleeMask;          // enemy and breakable stuff

    [SerializeField] private float attackCooldown = 0.35f;
    private float _nextAttackTime;
    
    [Header("Crouch")]
    private float _cameraYVelocity;
    public float crouchSmoothTime = 0.12f;
    public float crouchHeadOffset = -0.5f;
    [SerializeField] private Transform headTarget;
    
    [Header("Vignette")]
    [SerializeField] private VolumeProfile _volumeProfile;
    public float vignetteSmoothTime = 0.12f;
    private float _vignetteIntensity;
    
    
    
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private CinemachineCamera _cinCam;
    
    private Vector3 _headInitialLocalPos;
    private float _headYVelocity;
    
    private Vector2 _move;
    private Vector3 _velocity;

    public void Start()
    {
        currentSpeed = walkSpeed;
        _velocity = Vector3.zero;
        _headInitialLocalPos = headTarget.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        float Fr = 1, To = 10, I=2;
        float t = Mathf.InverseLerp(Fr, To, I);

        Debug.Log(Mathf.InverseLerp(Fr, To, I)); // 1/9 = 0.11 (часть пути) t = (2 - 1) / (10 - 1) = 1/9 (c-a)/(b-a)
        Debug.Log(Mathf.Lerp(Fr, To, t)); // a + (b - a) * t или же 1+(9*1.9) = 2 (значение пути)
    }
    
    public void Update()
    {
        if (_characterController.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        
        _velocity.y += gravity * Time.deltaTime;
        
        Vector3 move = ((GetForward() * _move.y + GetRight() * _move.x) * currentSpeed);
        
        _characterController.Move((move + _velocity) * Time.deltaTime);
        
        _volumeProfile.TryGet(out Vignette vignette);
        vignette.intensity.value = Mathf.SmoothDamp(
            vignette.intensity.value,
            isCrouching ? 0.25f : 0f,
            ref _vignetteIntensity,
            vignetteSmoothTime);
    }

    public void LateUpdate()
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
        if (val.Get<float>() > 0.5f && _characterController.isGrounded)
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

    private Vector3 GetForward()
    {
        Vector3 forward = _cinCam.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }
    
    private Vector3 GetRight()
    {
        Vector3 right = _cinCam.transform.right;
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
        if (val.Get<float>() > 0.5f)
            TryAttack();
        
    }

    
    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
            return;

        _nextAttackTime = Time.time + attackCooldown;

        Ray ray = new Ray(_cinCam.transform.position, _cinCam.transform.forward);

        // 1) Сначала пробуем ударить врага/цель SphereCast'ом (стабильнее чем Raycast)
        if (Physics.SphereCast(ray, meleeRadius, out RaycastHit hit, meleeRange, meleeMask, QueryTriggerInteraction.Ignore))
        {
            // --- ВРАГ ---
            EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
            if (enemy != null && !enemy.isDead)
            {
                if (IsBehindEnemy(enemy.transform))
                {
                    // Backstab todo: анимацию с ragdoll
                    enemy.Die();
                }
                else
                {
                    
                    enemy.TakeDamage(meleeDamage, hit.point, ray.direction);
                }

                Debug.DrawRay(ray.origin, ray.direction * meleeRange, Color.red, 0.2f);
                return;
            }

            
            BreakableDoor door = hit.collider.GetComponentInParent<BreakableDoor>();
            if (door != null)
            {
                door.ApplyDamage(meleeDamage, hit.point, ray.direction);
                Debug.DrawRay(ray.origin, ray.direction * meleeRange, Color.yellow, 0.2f);
                return;
            }
        }

        
        Debug.DrawRay(ray.origin, ray.direction * meleeRange, Color.gray, 0.2f);
    }

    private bool IsBehindEnemy(Transform enemyRoot)
    {
        // Направление, куда смотрит враг (только по XZ)
        Vector3 enemyForward = enemyRoot.forward;
        enemyForward.y = 0f;
        enemyForward.Normalize();

        // Направление от врага к игроку (XZ)
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
        Ray ray = new Ray(_cinCam.transform.position, _cinCam.transform.forward);

        
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            // 3) Ищем DoorAnimator на объекте или выше по иерархии
            DoorsAnim door = hit.collider.GetComponentInParent<DoorsAnim>();
            if (door != null)
            {
                door.Toggle();
            }
        }
        
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow, 0.2f);
    }
    
}
