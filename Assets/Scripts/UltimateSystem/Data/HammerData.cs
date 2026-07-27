using UnityEngine;

[CreateAssetMenu(menuName = "Ultimate/Debuffs/Global/Hammer")]
public class HammerData : ScriptableObject
{
    public Sprite wheelIcon;
    public float hitShakeIntensity = 1f;
    public float hitDuration = 0.3f;
    public int maxHits = 10;
    public float targetingTimeLimit = 8f;
}