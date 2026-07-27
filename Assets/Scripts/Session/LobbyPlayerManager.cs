using Unity.Netcode;
using UnityEngine;

public class LobbyPlayerManager : NetworkBehaviour
{
    public static LobbyPlayerManager Instance { get; private set; }

    public NetworkList<LobbyPlayerData> Players;

    void Awake()
    {
        Instance = this;
        Players = new NetworkList<LobbyPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // сам хост тоже игрок — добавляем сразу
            AddPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return; // хост уже добавлен
        AddPlayer(clientId);
    }

    void OnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Players.RemoveAt(i);
                break;
            }
        }
    }

    void AddPlayer(ulong clientId)
    {
        Players.Add(new LobbyPlayerData
        {
            ClientId = clientId,
            PlayerName = $"Player {clientId}",
            IsReady = clientId == NetworkManager.Singleton.LocalClientId && IsHost ? true : false
            // хост по умолчанию не обязан жать "готов" себе — но проверку старта всё равно делаем по всем НЕ-хостам
        });
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[Server] SetReadyServerRpc received: {ready} from {rpcParams.Receive.SenderClientId}");
        ulong senderId = rpcParams.Receive.SenderClientId;

        // защита: не даём стать готовым без выбранной роли
        if (ready && RoleAssignmentManager.Instance.GetRoleFor(senderId) == GameRole.None) return;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == senderId)
            {
                var data = Players[i];
                data.IsReady = ready;
                Players[i] = data;
                break;
            }
        }
    }

    public bool AllNonHostPlayersReady()
    {
        foreach (var p in Players)
        {
            if (p.ClientId == NetworkManager.Singleton.LocalClientId && IsHost) continue; // хост не считаем
            if (!p.IsReady) return false;
        }
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (!AllNonHostPlayersReady()) return;
        GameSessionState.Instance.SetPhase(SessionPhase.InGame); // было RoleSelect
    }

    public void ResetAllReady()
    {
        if (!IsServer) return;

        for (int i = 0; i < Players.Count; i++)
        {
            var p = Players[i];
            p.IsReady = false;
            Players[i] = p;
        }
    }
}