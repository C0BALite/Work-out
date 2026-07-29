using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class DebuffWheelNetwork : NetworkBehaviour
{
    public static DebuffWheelNetwork Instance { get; private set; }

    [SerializeField] private List<DebuffData> allDebuffs = new List<DebuffData>();
    public List<DebuffData> AllDebuffs => allDebuffs;

    void Awake()
    {
        Instance = this;
    }

    public DebuffData GetRandomDebuff()
    {
        if (allDebuffs == null || allDebuffs.Count == 0) return null;
        return allDebuffs[Random.Range(0, allDebuffs.Count)];
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyDebuffServerRpc(int debuffId, ulong targetId, ulong casterId)
    {
        DebuffData debuff = allDebuffs.Find(d => d.DebuffId == debuffId);
        if (debuff == null) return;

        DebuffManager.Instance?.ApplyDebuff(debuff, targetId, casterId);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(casterId, out var client))
        {
            var casterUlt = client.PlayerObject.GetComponent<UltimateSystem>();
            casterUlt?.ResetUltimate();
        }

        NotifyDebuffAppliedClientRpc(debuffId, targetId, debuff.debuffName);
    }

    [ClientRpc]
    void NotifyDebuffAppliedClientRpc(int debuffId, ulong targetId, string debuffName)
    {
        Debug.Log($"Дебаф {debuffName} применён к игроку {targetId}!");
    }
}