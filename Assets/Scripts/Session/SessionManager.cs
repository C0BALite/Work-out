using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public Lobby CurrentLobby { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<string> CreateLobbyAsync(string playerName)
    {
        var allocation = await RelayService.Instance.CreateAllocationAsync(4); // 4 = макс доп. игроков
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var lobbyOptions = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new System.Collections.Generic.Dictionary<string, DataObject>
            {
                { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            }
        };

        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync("game-lobby", 5, lobbyOptions);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartHost();

        return CurrentLobby.LobbyCode; // это игрок сообщает друзьям
    }

    public async Task JoinLobbyAsync(string lobbyCode)
    {
        CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

        string relayJoinCode = CurrentLobby.Data["relayJoinCode"].Value;
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls")); // было SetJoinRelayServerData

        NetworkManager.Singleton.StartClient();
    }
}