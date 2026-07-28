using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DebuffManager : NetworkBehaviour
{
    public static DebuffManager Instance { get; private set; }

    private Dictionary<ulong, List<ActiveDebuff>> activeDebuffs = new Dictionary<ulong, List<ActiveDebuff>>();
    private Dictionary<int, IDebuffEffect> debuffEffects = new Dictionary<int, IDebuffEffect>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        RegisterDebuffEffects();
    }

    private void RegisterDebuffEffects()
    {
        // Здесь регистрируй свои дебафы по мере создания
        // debuffEffects[someDebuff.DebuffId] = new SlowEffect();
    }

    public void ApplyDebuff(DebuffData debuffData, ulong targetId, ulong casterId)
    {
        if (!IsServer) return;

        ActiveDebuff activeDebuff = new ActiveDebuff
        {
            Data = debuffData,
            TargetId = targetId,
            CasterId = casterId,
            RemainingTime = debuffData.duration,
            IsActive = true
        };

        if (!activeDebuffs.ContainsKey(targetId))
            activeDebuffs[targetId] = new List<ActiveDebuff>();

        activeDebuffs[targetId].Add(activeDebuff);

        if (debuffEffects.TryGetValue(debuffData.DebuffId, out IDebuffEffect effect))
        {
            effect.Apply(targetId);
        }

        StartCoroutine(RemoveDebuffAfterDelay(debuffData.DebuffId, targetId, debuffData.duration));
        SpawnVisualEffectClientRpc(debuffData.DebuffId, targetId);
    }

    private IEnumerator RemoveDebuffAfterDelay(int debuffId, ulong targetId, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveDebuff(debuffId, targetId);
    }

    public void RemoveDebuff(int debuffId, ulong targetId)
    {
        if (!IsServer) return;

        if (activeDebuffs.ContainsKey(targetId))
        {
            ActiveDebuff debuff = activeDebuffs[targetId].Find(d => d.Data.DebuffId == debuffId);
            if (debuff != null)
            {
                if (debuffEffects.TryGetValue(debuffId, out IDebuffEffect effect))
                    effect.Remove(targetId);

                activeDebuffs[targetId].Remove(debuff);
                RemoveVisualEffectClientRpc(debuffId, targetId);
            }
        }
    }

    [ClientRpc]
    private void SpawnVisualEffectClientRpc(int debuffId, ulong targetId)
    {
        // Визуальный эффект спавним здесь
    }

    [ClientRpc]
    private void RemoveVisualEffectClientRpc(int debuffId, ulong targetId)
    {
        // Убираем визуальный эффект
    }

    public bool HasDebuff(ulong playerId, int debuffId)
    {
        if (activeDebuffs.TryGetValue(playerId, out var debuffs))
            return debuffs.Exists(d => d.Data.DebuffId == debuffId);
        return false;
    }
}