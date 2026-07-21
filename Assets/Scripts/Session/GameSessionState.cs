using Unity.Netcode;
using UnityEngine;

public enum SessionPhase { Lobby, RoleSelect, InGame, Results }

public class GameSessionState : NetworkBehaviour
{
    public static GameSessionState Instance { get; private set; }

    public NetworkVariable<SessionPhase> Phase = new NetworkVariable<SessionPhase>(
        SessionPhase.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Phase.OnValueChanged += (oldPhase, newPhase) =>
        {
            Debug.Log($"[Session] Phase changed: {oldPhase} -> {newPhase}");
        };
    }

    // вызывается только сервером
    public void SetPhase(SessionPhase newPhase)
    {
        if (!IsServer) return;
        Phase.Value = newPhase;
    }
}