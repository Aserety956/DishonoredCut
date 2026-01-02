using System;
using System.Text.RegularExpressions;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    public float currentSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float gravity;
    private bool _isCrouching;
    private float _cameraYVelocity;
    private Vector3 _cameraInitialLocalPos;
    [SerializeField] private float crouchSmoothTime = 0.12f;
    [SerializeField] private float crouchCameraOffset = -0.5f;
    
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private CinemachineCamera _cinCam;

    private Vector2 _move;
    private Vector3 _velocity;

    public void Start()
    {
        currentSpeed = walkSpeed;
        _velocity = Vector3.zero;
        _cameraInitialLocalPos = _cinCam.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void Update()
    {
        _velocity.y += gravity * Time.deltaTime;
        
        Vector3 move = ((GetForward() * _move.y + GetRight() * _move.x) * currentSpeed);
        
        _characterController.Move((move + _velocity) * Time.deltaTime);
        
        
        float targetHeight = _isCrouching ? 1f : 2f;
        
        _characterController.height = Mathf.MoveTowards(
            _characterController.height,
            targetHeight,
            2f * Time.deltaTime
        );
        
        
        float targetOffset = _isCrouching ? crouchCameraOffset : 0f;

        Vector3 camLocalPos = _cinCam.transform.localPosition;
        
            camLocalPos.y = Mathf.SmoothDamp(
            camLocalPos.y,
            _cameraInitialLocalPos.y + targetOffset,
            ref _cameraYVelocity,
            crouchSmoothTime
        );

        _cinCam.transform.localPosition = camLocalPos;
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
            _isCrouching = true;
        }
        else
        {
            _isCrouching = false;
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
    
    
}
