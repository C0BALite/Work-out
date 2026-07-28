using UnityEngine;
using Unity.Netcode;

public class SlowEffect : IDebuffEffect
{
    private float slowFactor = 0.5f;

    public void Apply(ulong targetClientId)
    {
        GameObject playerObj = GetPlayerObject(targetClientId);
        if (playerObj != null)
        {
            // Здесь потом добавишь свою логику замедления
            // Например: playerObj.GetComponent<PlayerMovement>().Speed *= slowFactor;
        }

        Debug.Log($"Игрок {targetClientId} замедлен!");
    }

    public void Remove(ulong targetClientId)
    {
        GameObject playerObj = GetPlayerObject(targetClientId);
        if (playerObj != null)
        {
            // Здесь уберёшь замедление
            // Например: playerObj.GetComponent<PlayerMovement>().Speed /= slowFactor;
        }

        Debug.Log($"Игрок {targetClientId} больше не замедлен!");
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