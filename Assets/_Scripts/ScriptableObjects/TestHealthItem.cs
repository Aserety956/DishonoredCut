using UnityEngine;

[CreateAssetMenu(menuName = "Game/QuickItems/Test Heal")]
public class TestHealItem : QuickItem
{
    public int healAmount = 25;

    public override void Use(GameObject user)
    {
        Debug.Log($"{user.name} used {displayName} and healed {healAmount}");
        // Позже: user.GetComponent<Health>().Heal(healAmount);
    }
}
