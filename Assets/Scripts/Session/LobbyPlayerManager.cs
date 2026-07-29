using Unity.Netcode;
using UnityEngine;

public class LobbyPlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerAbilityPrefab; // новое — UltimateSystem + DebuffReceiver
    public static LobbyPlayerManager Instance { get; private set; }

    public NetworkList<LobbyPlayerData> Players;

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[LobbyPlayerManager] OnClientConnected fired for clientId={clientId}");
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[LobbyPlayerManager] Skipped — this is the host itself");
            return;
        }
        AddPlayer(clientId);
        SpawnPlayerAbilityObject(clientId);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            AddPlayer(NetworkManager.Singleton.LocalClientId);
            SpawnPlayerAbilityObject(NetworkManager.Singleton.LocalClientId); // новое — и для хоста тоже
        }
    }

    void SpawnPlayerAbilityObject(ulong clientId)
    {
        Debug.Log($"[LobbyPlayerManager] SpawnPlayerAbilityObject CALLED with clientId={clientId}, LocalClientId={NetworkManager.Singleton.LocalClientId}");

        var obj = Instantiate(playerAbilityPrefab);
        var netObj = obj.GetComponentInChildren<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);

        Debug.Log($"[LobbyPlayerManager] Spawned for clientId={clientId}, OwnerClientId={netObj.OwnerClientId}, IsSpawned={netObj.IsSpawned}");
    }
    void Awake()
    {
        Instance = this;
        Players = new NetworkList<LobbyPlayerData>();
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