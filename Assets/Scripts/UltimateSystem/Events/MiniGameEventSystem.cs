using Unity.Netcode;
using UnityEngine;

public class MiniGameEventSystem : NetworkBehaviour
{
    public static MiniGameEventSystem Instance { get; private set; }

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

    public void ReportCorrectAction(ulong playerId)
    {
        if (!IsServer)
        {
            ReportCorrectActionServerRpc(playerId);
            return;
        }
        ProcessCorrectAction(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportCorrectActionServerRpc(ulong playerId)
    {
        ProcessCorrectAction(playerId);
    }

    private void ProcessCorrectAction(ulong playerId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
        {
            UltimateSystem ultimate = client.PlayerObject.GetComponent<UltimateSystem>();
            if (ultimate != null)
            {
                ultimate.AddProgressForCorrectAction();
                Debug.Log($"MiniGame reported correct action for player {playerId}");
            }

            // новое — +10 очков за правильное действие + учёт для среднего бонуса боссу в конце раунда
            GameSessionState.Instance?.RegisterCorrectAction(playerId);
        }
    }
}