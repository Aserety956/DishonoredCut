using System;
using UnityEngine;
using UnityEngine.UI;

public class SuspicionBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private EnemyController enemy;
    [SerializeField] private Animator barAnim;
    


    private Camera cam;
    
    private static readonly int Suspicion = Animator.StringToHash("Suspicion");

    
    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.OnEnemyDead += DisableBar;
            enemy.OnEnemyKnocked += DisableBar;
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnEnemyDead -= DisableBar;
            enemy.OnEnemyKnocked -= DisableBar;
        }
    }
    
    
    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {

        // Поворачиваем UI лицом к камере
        transform.rotation = Quaternion.LookRotation
            (transform.position - cam.transform.position);
    }
    
    void Update()
    {
        float suspicion01 = enemy.suspicion;
        
        barAnim.SetFloat(Suspicion, suspicion01, 0, Time.deltaTime);
        
        float susp = enemy.suspicion;
        
        fillImage.fillAmount = susp;
        
        
        switch (susp)
        {
            case <= 0f:
                fillImage.gameObject.SetActive(false);
                backgroundImage.gameObject.SetActive(false);
                barAnim.enabled = false;
                break;
            
            case < 0.99f:
                fillImage.gameObject.SetActive(true);
                backgroundImage.gameObject.SetActive(true);
                barAnim.enabled = true;
                break;
            
            case >= 1f:
                fillImage.gameObject.SetActive(false);
                backgroundImage.gameObject.SetActive(false);
                barAnim.enabled = false;
                break;
                // Todo: доделать анимация ААА задетектили
        }
        
    }
    
    private void DisableBar()
    {
        gameObject.SetActive(false);
    }

}
