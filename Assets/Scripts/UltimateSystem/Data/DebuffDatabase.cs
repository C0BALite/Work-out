using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ultimate/Debuff Database")]
public class DebuffDatabase : ScriptableObject
{
    [Header("Дизайнер")]
    public List<DebuffData> designerDebuffs;
    [Header("Маркетолог")]
    public List<DebuffData> marketerDebuffs;
    [Header("Копирайтер")]
    public List<DebuffData> copywriterDebuffs;

    public DebuffData GetRandomForRole(PlayerRole role)
    {
        List<DebuffData> list = role switch
        {
            PlayerRole.Designer => designerDebuffs,
            PlayerRole.Marketer => marketerDebuffs,
            PlayerRole.Copywriter => copywriterDebuffs,
            _ => null
        };
        return list != null && list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }
}