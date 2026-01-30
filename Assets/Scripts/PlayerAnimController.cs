using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 5f; // как в PlayerController
    [SerializeField] private float runSpeed = 10f;  // как в PlayerController

    [Header("Smoothing")]
    [SerializeField] private float dampTime = 0.12f;

    private static readonly int Speed01 = Animator.StringToHash("Speed01");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    
    private void Update()
    {
        
        Vector3 v = controller.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        // 2) Нормализуем в 0..1:
        // 0..walkSpeed -> 0..0.5
        // walkSpeed..runSpeed -> 0.5..1
        float speed01;
        if (speed <= walkSpeed)
            speed01 = Mathf.InverseLerp(0f, walkSpeed, speed) * 0.5f;
        else
            speed01 = 0.5f + Mathf.InverseLerp(walkSpeed, runSpeed, speed) * 0.5f;
        
        animator.SetFloat(Speed01, speed01, dampTime, Time.deltaTime);

        // 4) Grounded (полезно для прыжка/падения, даже если пока нет)
       // animator.SetBool(IsGrounded, controller.isGrounded);
    }
}
