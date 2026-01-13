using System;
using UnityEngine;
using UnityEngine.UI;

public class SuspicionBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private EnemyController enemy;
    


    void Update()
    {
        
        fillImage.fillAmount = enemy.suspicion;
        
        if (fillImage.fillAmount <= 0f)
        {
            fillImage.gameObject.SetActive(false);
            backgroundImage.gameObject.SetActive(false);
            
        }
        else
        {
            fillImage.gameObject.SetActive(true);
            backgroundImage.gameObject.SetActive(true);
        }

        if (fillImage.fillAmount <= 0.5f)
        {
            fillImage.color = Color.white;
        }
        if (fillImage.fillAmount >= 0.5f)
        {
            fillImage.color = Color.yellow;
        }
        // todo: анимация ААА задетектили
        if (fillImage.fillAmount >= 1f)
        {
            fillImage.color = Color.red;
            fillImage.gameObject.SetActive(false);
            backgroundImage.gameObject.SetActive(false);
        }
    }
}
