using UnityEngine;

public abstract class QuickItem : ScriptableObject
{
    public string displayName;
    public Sprite icon;

    // Позже сюда можно добавить: cooldown, stack size, тип, и т.д.
    public abstract void Use(GameObject user);
}
