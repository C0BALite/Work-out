using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum GameRole { None, Boss, Typographer, Artist, Programmer }

public struct PlayerRoleData : INetworkSerializable, System.IEquatable<PlayerRoleData>
{
    public ulong ClientId;
    public GameRole Role;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Role);
    }

    public bool Equals(PlayerRoleData other) => ClientId == other.ClientId && Role == other.Role;
}

public class RoleAssignmentManager : NetworkBehaviour
{
    public static RoleAssignmentManager Instance { get; private set; }

    public NetworkList<PlayerRoleData> Assignments;

    void Awake()
    {
        Instance = this;
        Assignments = new NetworkList<PlayerRoleData>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRoleServerRpc(GameRole requestedRole, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[Server] RequestRoleServerRpc received: {requestedRole} from {rpcParams.Receive.SenderClientId}");
        if (requestedRole == GameRole.Boss || requestedRole == GameRole.None) return; // нельзя выбрать вручную

        ulong senderId = rpcParams.Receive.SenderClientId;
        if (IsRoleTaken(requestedRole)) return;

        for (int i = 0; i < Assignments.Count; i++)
        {
            if (Assignments[i].ClientId == senderId)
            {
                if (Assignments[i].Role == GameRole.Boss) return; // босс не может себе сменить роль
                Assignments[i] = new PlayerRoleData { ClientId = senderId, Role = requestedRole };
                return;
            }
        }

        Assignments.Add(new PlayerRoleData { ClientId = senderId, Role = requestedRole });
    }
    public void AssignBossToHost(ulong hostClientId)
    {
        if (!IsServer) return;

        // защита от дублирования, если вызвано повторно
        for (int i = 0; i < Assignments.Count; i++)
        {
            if (Assignments[i].ClientId == hostClientId) return;
        }

        Assignments.Add(new PlayerRoleData { ClientId = hostClientId, Role = GameRole.Boss });
    }
    public bool IsRoleTaken(GameRole role)
    {
        foreach (var a in Assignments)
            if (a.Role == role) return true;
        return false;
    }

    public GameRole GetRoleFor(ulong clientId)
    {
        foreach (var a in Assignments)
            if (a.ClientId == clientId) return a.Role;
        return GameRole.None;
    }

    public GameRole GetMyRole() => GetRoleFor(NetworkManager.Singleton.LocalClientId);
}