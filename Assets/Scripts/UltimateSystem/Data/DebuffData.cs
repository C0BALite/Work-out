using UnityEngine;

public abstract class DebuffData : ScriptableObject
{
    public string debuffName;
    public float duration = 5f;
    public Sprite icon;

    public abstract void Apply(PlayerState target);
    public abstract void Remove(PlayerState target);
}