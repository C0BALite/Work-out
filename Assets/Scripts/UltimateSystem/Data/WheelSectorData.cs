using UnityEngine;

public enum SectorType { Global, RoleRandom }

[CreateAssetMenu(menuName = "Ultimate/Wheel Sector")]
public class WheelSectorData : ScriptableObject
{
    public Sprite wheelIcon;
    public string displayName;
    public Color sectorColor = Color.white;
    public SectorType type;

    [Header("Если Global")]
    public DebuffData globalDebuff;

    [Header("Если RoleRandom")]
    public Sprite roleRandomIcon;
}