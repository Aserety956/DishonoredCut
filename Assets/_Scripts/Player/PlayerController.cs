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
using UnityEngine.VFX;

public class PlayerController : MonoBehaviour
{
// private (_doSomething) // public (DoSomething) // static s(_Public, _private)
// interface (IDoSomething)
    [Header("Movement")]
    public float currentSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float gravity;
    public bool isCrouching;
    public bool isSprinting;
    public bool isSwordEquipped;
    public float jumpHeight;
    
    [Header("Interact")]
    public float interactDistance = 3f;
    public LayerMask interactMask;
    public float throwForce; // на будущее: сила зависит от навыка
    private IInteractable _currentInteractable;
    [SerializeField] private InteractPromptUI interactPromptUI;
    
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
    //[SerializeField] private Image backgroundImage;
    [SerializeField] private RadialMenu radialMenu;
    [SerializeField] private QuickbarManager quickbar;

    [Header("Stats")] 
    public float currentHP;
    public float currentMP;
    public float baseHP;
    public float baseMP;
    
    [Header("Cinemachine")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera cinCam;
    private BottleItem _heldItem;
    [SerializeField] private Transform handsPos;
    private Vector3 _handsInitialLocalPos;
    [SerializeField] private Vector3 handsHoldOffset = new Vector3(0f, -1f, 0f); 
    [SerializeField] private Transform itemPos;

    [Header("Sound")]
    [SerializeField] private SoundData footstepSound;
    [SerializeField] private SoundData crouchSound;
    [SerializeField] private Transform feetPosition;
    
    [SerializeField] private float stepInterval1 = 0.28f; // 1-й шаг (A)
    [SerializeField] private float stepInterval2 = 0.30f; // 2-й шаг (B) - чуть больше/меньше для синхры

    [SerializeField] private float runStep1 = 0.20f;
    [SerializeField] private float runStep2 = 0.22f;
    
    [SerializeField] private float minSpeedToStep = 0.15f; // порог движения
    [SerializeField] private float stopResetTime = 0.08f;  // чтобы быстро сбрасывать цикл на шаг 1
    
    [SerializeField] private float startMoveDelay = 0.12f;
    private float _moveTimer;

    private float _stepTimer;
    private bool _secondStep;      // false -> шаг1, true -> шаг2
    private float _stopTimer;
    
    [Header("Debug")]
    [SerializeField] private GameObject itemPrefab;
    private Vector3 offsetToSpawn;
    [SerializeField] private VisualEffect rainVFX;
    [SerializeField] private ParticleSystem rain;
    [SerializeField] private SoundData rainSound;
    private bool _rainEnabled;
    private Coroutine _rainRoutine;
    
    
    private Vector3 _headInitialLocalPos;
    private float _headYVelocity;
    
    private Vector2 _move;
    private Vector3 _velocity;
    
    
    public void Awake()
    {
        rainVFX.SetFloat("RainAmount", 0f);
    }
    
    public void Start()
    {
        baseHP = 100f;
        baseMP = 100f;
        currentHP = baseHP;
        currentMP = baseMP;
        
        currentSpeed = walkSpeed;
        _velocity = Vector3.zero;
        _headInitialLocalPos = headTarget.localPosition;
        _handsInitialLocalPos = handsPos.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        HealthUpdate();
        ManaUpdate();
    }
    
    public void Update()
    {

        UpdateMove();

        UpdateAttack();

        UpdateInteractHover();

        RadialMenuUpdate();

    }

    
    
    public void LateUpdate()
    {
        
        UpdateCam();
        
    }
    
    private void UpdateHandsPose()
    {
        handsPos.localPosition = (_heldItem != null)
            ? _handsInitialLocalPos + handsHoldOffset
            : _handsInitialLocalPos;
    }
    
    public void UpdateMove()
    {
        if (characterController.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        
        _velocity.y += gravity * Time.deltaTime;
        
        Vector3 move = ((GetForward() * _move.y + GetRight() * _move.x) * currentSpeed);
        
        characterController.Move((move + _velocity) * Time.deltaTime);
        
        TickFootstepsTwoTimers();
        
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
        if (val.Get<float>() > 0.5f && !isCrouching)
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
        }
        else
        {
            isSprinting = false;
            currentSpeed = walkSpeed;
            
        }
    }
    
    public void OnJump(InputValue val)
    {
        if (val.Get<float>() > 0.5f && characterController.isGrounded)
        {
            _velocity.y = jumpHeight; 
        }
    }
    
    public void OnCrouch(InputValue val)
    {
        if (val.Get<float>() > 0.5f)
        {
            isCrouching = true;
            currentSpeed = crouchSpeed;
        }
        else
        {
            isCrouching = false;
            currentSpeed = walkSpeed;
        }
    }

    public void OnDebugBottle(InputValue val)
    {
        if (val.Get<float>() > 0.5f)
        {
            offsetToSpawn = cinCam.transform.forward * 2;
            offsetToSpawn.y = 1;
            Instantiate(itemPrefab, transform.position + offsetToSpawn, transform.rotation);
        }
    }

    public void OnDebugRain(InputValue val)
    {
        if (val.Get<float>() <= 0.5f)
            return;
        
        RainManager();
    }
    
    public void RainManager()
    {
        _rainEnabled = !_rainEnabled;


        _rainRoutine = StartCoroutine(RainVFXCoroutine(_rainEnabled ? 100f : 0f, 3f));

        if (_rainEnabled)
            AudioManager.I.PlayAmbience(rainSound);
        else
            AudioManager.I.StopAmbience(rainSound);
    }

    public IEnumerator RainVFXCoroutine(float amount, float duration)
    {
        
        float startAmount = rainVFX.GetFloat("RainAmount");
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float value = Mathf.Lerp(startAmount, amount, time / duration);
            rainVFX.SetFloat("RainAmount", value);
            yield return null;
        }
        rainVFX.SetFloat("RainAmount", amount);
    }
    
    /*public void OnRadialMenu(InputValue val)
    {
        if (val.isPressed)
        {
            characterController.enabled = false;
            radialMenu.Open();
        }
        else
        {
            characterController.enabled = true;
            radialMenu.Close();
            
        }
    }*/

    public void RadialMenuUpdate()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            //characterController.enabled = false;
            radialMenu.Open();
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            //characterController.enabled = true;
            radialMenu.Close();
        }
    }

    public void OnSlot1(InputValue v) { if (v.isPressed) quickbar.Select(0); }
    public void OnSlot2(InputValue v) { if (v.isPressed) quickbar.Select(1); }
    public void OnSlot3(InputValue v) { if (v.isPressed) quickbar.Select(2); }
    public void OnSlot4(InputValue v) { if (v.isPressed) quickbar.Select(3); }
    public void OnSlot5(InputValue v) { if (v.isPressed) quickbar.Select(4); }
    public void OnSlot6(InputValue v) { if (v.isPressed) quickbar.Select(5); }
    public void OnSlot7(InputValue v) { if (v.isPressed) quickbar.Select(6); }
    public void OnSlot8(InputValue v) { if (v.isPressed) quickbar.Select(7); }

    public void OnUseQuick(InputValue v) { if (v.isPressed) quickbar.UseSelected(); }
    
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
            TryInteract();
            
        }
    }

    public void OnAttack(InputValue val)
    {
        if (!val.isPressed) return;

        if (_heldItem == null)
            TryAttack();

        else
            ThrowHeldItem();


    }
    
    public void ThrowHeldItem()
    {
        Vector3 velocityChange = cinCam.transform.forward * throwForce;
        _heldItem.ThrowFrom(itemPos.position,itemPos.rotation,velocityChange);
        _heldItem = null;
        UpdateHandsPose();
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
            if (IsBehindEnemy(enemy.transform) && !enemy._knocked)
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

    
    public void PickupItem(BottleItem item)
    {
        if (item == null)
            return;

        if (_heldItem != null)
        {
            _heldItem.ReleaseDrop(itemPos.position, itemPos.rotation);
        }

        _heldItem = item;
        _heldItem.PickupTo(itemPos, Vector3.zero, Vector3.zero);
        UpdateHandsPose();
    }
    
    private void TryInteract()
    {
        if (_heldItem != null)
        {
            _heldItem.ReleaseDrop(itemPos.position, itemPos.rotation);
            _heldItem = null;
            UpdateHandsPose();
            ClearCurrentInteractable();
            return;
        }

        Ray ray = new Ray(cinCam.transform.position, cinCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                UpdateInteractPrompt(interactable);
                return;
            }
        }

        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow, 0.2f);
    }
    
    
    private void TickFootstepsTwoTimers()
    {
        if (AudioManager.I == null || footstepSound == null) return;

        // Только на земле
        if (!characterController.isGrounded)
        {
            ResetFootstepsCycle();
            return;
        }

        // Реальная скорость (XZ) из CharacterController
        Vector3 v = characterController.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        // Если стоим — сбросим цикл (чтобы при старте всегда был "шаг 1")
        if (speed < minSpeedToStep)
        {
            _stopTimer += Time.deltaTime;
            if (_stopTimer >= stopResetTime)
                ResetFootstepsCycle();
            _moveTimer = 0f;
            return;
        }

        _stopTimer = 0f;
        
        _moveTimer += Time.deltaTime;
        
        if (_moveTimer < startMoveDelay)
            return;
        
        
        bool run = isSprinting; 
        bool crouch = isCrouching;
        float step1 = run ? runStep1 : stepInterval1;
        float step2 = run ? runStep2 : stepInterval2;

        if (crouch)
        {
            step1 *= 1.5f;
            step2 *= 1.5f;
        }
        
        float interval = _secondStep ? step2 : step1;
        
        SoundData step = crouch ? crouchSound : footstepSound; 

        _stepTimer -= Time.deltaTime;
        
        if (_stepTimer <= 0f)
        {
            AudioManager.I.Play(step, transform.position);

            // Следующий шаг: переключаем 1<->2
            _secondStep = !_secondStep;

            // Таймер на следующий интервал
            _stepTimer += interval;
        }
    }

    private void ResetFootstepsCycle()
    {
        _stepTimer = 0f;
        _secondStep = false; // чтобы первый шаг всегда был "шаг 1"
        _stopTimer = 0f;
        _moveTimer = 0f;
    }
    
    private void UpdateInteractHover()
    {
        Ray ray = new Ray(cinCam.transform.position, cinCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                if (_currentInteractable == interactable)
                {
                    UpdateInteractPrompt(interactable);
                    return;
                }

                ClearCurrentInteractable();

                _currentInteractable = interactable;
                _currentInteractable.SetHighlight(true);
                UpdateInteractPrompt(_currentInteractable);
                return;
            }
        }

        ClearCurrentInteractable();
    }

    private void ClearCurrentInteractable()
    {
        if (_currentInteractable != null)
        {
            _currentInteractable.SetHighlight(false);
            _currentInteractable = null;
        }

        if (interactPromptUI != null)
            interactPromptUI.Hide();
    }
    
    private void UpdateInteractPrompt(IInteractable interactable)
    {
        if (interactPromptUI == null)
            return;

        if (interactable == null)
        {
            interactPromptUI.Hide();
            return;
        }

        string text = interactable.GetInteractText();

        if (string.IsNullOrWhiteSpace(text))
            interactPromptUI.Hide();
        else
            interactPromptUI.Show(text);
    }
}
