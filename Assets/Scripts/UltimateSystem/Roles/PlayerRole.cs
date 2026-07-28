using Unity.Netcode;
using UnityEngine;

public class PlayerRole : NetworkBehaviour
{
    private NetworkVariable<Role> role = new NetworkVariable<Role>(Role.Boss);
    public Role CurrentRole => role.Value;
    public bool IsBoss => CurrentRole == Role.Boss;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int playerIndex = (int)OwnerClientId;
            Role assignedRole = (Role)(playerIndex % 4);
            role.Value = assignedRole;
            Debug.Log($"Player {OwnerClientId} assigned role: {assignedRole}");
        }
    }
}