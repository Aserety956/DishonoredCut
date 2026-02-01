using System;
using System.Collections;
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
    private float _nextAttackTime;
    [SerializeField] private float attackHitDelay;   // когда реально "попадает"
    [SerializeField] private float attackTotalTime;  // длительность анимации
    private bool _isAttacking;
    private Coroutine _attackRoutine;
    
    [Header("Animation")] // attack animation
    [SerializeField] private Animator animator;
    private static readonly int AttackTrig = Animator.StringToHash("Attack");
    
    
    [Header("Vignette")]
    [SerializeField] private VolumeProfile _volumeProfile;
    public float vignetteSmoothTime = 0.12f;
    private float _vignetteIntensity;
    
    [Header("CrouchLogic")]
    private float _cameraYVelocity;
    public float crouchSmoothTime = 0.12f;
    public float crouchHeadOffset = -0.5f;
    [SerializeField] private Transform headTarget;
    
    
    
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

        if (_isAttacking)
            return;
        
        animator.SetTrigger(AttackTrig);
        
        _attackRoutine = StartCoroutine(AttackRoutine());
    }
    
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        // 1) ждём до "кадра попадания"
        yield return new WaitForSeconds(attackHitDelay);

        // 2) наносим урон именно здесь
        DoMeleeHit();

        // 3) ждём до конца удара (чтобы нельзя было спамить)
        float remaining = Mathf.Max(0f, attackTotalTime - attackHitDelay);
        yield return new WaitForSeconds(remaining);

        _isAttacking = false;
        _attackRoutine = null;
    }
    
    private void DoMeleeHit()
    {
        Ray ray = new Ray(_cinCam.transform.position, _cinCam.transform.forward);

        if (Physics.SphereCast(ray, meleeRadius, out RaycastHit hit, meleeRange, meleeMask, QueryTriggerInteraction.Ignore))
        {
            EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
            if (enemy != null /* && !enemy.isDead */)
            {
                if (IsBehindEnemy(enemy.transform))
                    enemy.Die();
                else
                    enemy.TakeDamage(meleeDamage, hit.point, ray.direction);

                return;
            }

            BreakableDoor door = hit.collider.GetComponentInParent<BreakableDoor>();
            if (door != null)
            {
                door.ApplyDamage(meleeDamage, hit.point, ray.direction);
                return;
            }
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
