using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DebuffManager : NetworkBehaviour
{
    public static DebuffManager Instance { get; private set; }

    private Dictionary<ulong, List<ActiveDebuff>> activeDebuffs = new Dictionary<ulong, List<ActiveDebuff>>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ApplyDebuff(DebuffData debuffData, ulong targetId, ulong casterId)
    {
        if (!IsServer) return;

        IDebuffEffect effect = debuffData.effectKind switch
        {
            DebuffEffectKind.Blur => new ScreenBlurEffect(),
            DebuffEffectKind.Slow => new SlowEffect(),
            _ => null
        };
        if (effect == null) return;

        var activeDebuff = new ActiveDebuff
        {
            Data = debuffData,
            TargetId = targetId,
            CasterId = casterId,
            RemainingTime = debuffData.duration,
            IsActive = true,
            Effect = effect
        };

        if (!activeDebuffs.ContainsKey(targetId))
            activeDebuffs[targetId] = new List<ActiveDebuff>();
        activeDebuffs[targetId].Add(activeDebuff);

        effect.Apply(targetId);

        StartCoroutine(RemoveDebuffAfterDelay(activeDebuff, debuffData.duration));
    }

    IEnumerator RemoveDebuffAfterDelay(ActiveDebuff debuff, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveDebuff(debuff);
    }

    void RemoveDebuff(ActiveDebuff debuff)
    {
        if (!IsServer) return;
        if (activeDebuffs.TryGetValue(debuff.TargetId, out var list) && list.Contains(debuff))
        {
            debuff.Effect.Remove(debuff.TargetId);
            list.Remove(debuff);
        }
    }

    public bool HasDebuff(ulong playerId, int debuffId)
    {
        if (activeDebuffs.TryGetValue(playerId, out var debuffs))
            return debuffs.Exists(d => d.Data.DebuffId == debuffId);
        return false;
    }
}