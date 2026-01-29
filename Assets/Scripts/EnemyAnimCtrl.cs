using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimCtrl : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Tooltip("Сколько секунд сглаживать изменение Speed параметра.")]
    [SerializeField] private float dampTime = 0.1f;

    private static readonly int Speed = Animator.StringToHash("Speed");
    

    private void Update()
    {
        // Нормализуем скорость: 0..1
        float speed01 = 0f;
        if (agent.speed > 0.001f)
            speed01 = agent.velocity.magnitude / agent.speed;

        animator.SetFloat(Speed, speed01, dampTime, Time.deltaTime);
    }
}
