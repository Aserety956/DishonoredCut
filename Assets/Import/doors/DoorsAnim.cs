using UnityEngine;

public class DoorsAnim : MonoBehaviour
{
    private Animator animator;
    private bool isOpen;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
    }
}
