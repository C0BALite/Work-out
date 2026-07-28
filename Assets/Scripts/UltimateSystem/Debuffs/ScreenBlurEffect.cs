using UnityEngine;
using Unity.Netcode;

public class ScreenBlurEffect : IDebuffEffect
{
    public void Apply(ulong targetClientId)
    {
        GameObject playerObj = GetPlayerObject(targetClientId);
        if (playerObj != null)
        {
            DebuffReceiver receiver = playerObj.GetComponent<DebuffReceiver>();
            if (receiver != null)
                receiver.ApplyBlur();
        }
    }

    public void Remove(ulong targetClientId)
    {
        GameObject playerObj = GetPlayerObject(targetClientId);
        if (playerObj != null)
        {
            DebuffReceiver receiver = playerObj.GetComponent<DebuffReceiver>();
            if (receiver != null)
                receiver.RemoveBlur();
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