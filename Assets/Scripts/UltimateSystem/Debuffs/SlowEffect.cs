using UnityEngine;
using Unity.Netcode;

public class SlowEffect : IDebuffEffect
{
    public void Apply(ulong targetClientId)
    {
        GameObject playerObj = GetPlayerObject(targetClientId);
        if (playerObj != null)
        {
            DebuffReceiver receiver = playerObj.GetComponent<DebuffReceiver>();
            if (receiver != null)
                receiver.ApplySlow();
        }
    }

    public void Remove(ulong targetClientId)
    {
        GameObject playerObj = GetPlayerObject(targetClientId);
        if (playerObj != null)
        {
            DebuffReceiver receiver = playerObj.GetComponent<DebuffReceiver>();
            if (receiver != null)
                receiver.RemoveSlow();
        }
    }

    private GameObject GetPlayerObject(ulong clientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            return client.PlayerObject != null ? client.PlayerObject.gameObject : null;
        }
        return null;
    }
}
