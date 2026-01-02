using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 1.0f;
    public Vector3 playerVelocity;
    public CharacterController controller;
    public bool groundedPlayer;
    public float gravityValue;
    public GameObject activeChar;
    float moveHorizontal;
    float moveVertical;
    public float playerSpeed = 4.0f;
    public float rotationSpeed = 4.0f;
    public float jumpHeight = 4.0f;
    public bool isJumping;
    
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    
    
    void Start()
    {
        playerSpeed = 4.0f;
        gravityValue = -20.0f;
        
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
    }

    
    void Update()
    {
        groundedPlayer = controller.isGrounded;
        
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }
        
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        transform.Rotate(0, 0, moveInput.x * rotationSpeed);
        
        Vector3 forward = transform.TransformDirection(Vector3.forward);

        float curSpeed = speed * moveInput.y;

        controller.SimpleMove(forward * curSpeed);

        if (jumpAction.triggered && groundedPlayer)
        {
            isJumping = true;
            GetComponent<Animator>().Play("Jump");
            playerVelocity.y += 10.0f; 
        }
        
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
        
        if(Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
        {
            this.gameObject.GetComponent<CharacterController>().minMoveDistance = 0.1f;
            if (isJumping == false)
            {
                activeChar.GetComponent<Animator>().Play("Standart Run");
            }
            
            else
            {
                this.gameObject.GetComponent<CharacterController>().minMoveDistance = 0.901f;
                
                if (isJumping == false)
                {
                    activeChar.GetComponent<Animator>().Play("Idle");
                }
            }
                
        }

        if (controller.isGrounded == true)
        {
            
        }
        
        else
        
        {
            
        }
        
        
        
        
        
        
    }
}
