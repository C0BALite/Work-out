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

    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float roundDuration = 120f; // это поле НА ПРЕФАБЕ — можно назначать в инспекторе Prefab-ассета

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Phase.OnValueChanged += (oldPhase, newPhase) =>
        {
            Debug.Log($"[Session] Phase changed: {oldPhase} -> {newPhase}");
            if (IsServer && newPhase == SessionPhase.InGame)
            {
                TimeRemaining.Value = roundDuration;
            }
        };
    }

    void Update()
    {
        if (!IsServer || Phase.Value != SessionPhase.InGame) return;

        TimeRemaining.Value -= Time.deltaTime;
        if (TimeRemaining.Value <= 0f)
        {
            TimeRemaining.Value = 0f;
            SetPhase(SessionPhase.Results);
        }
    }

    public void SetPhase(SessionPhase newPhase)
    {
        if (!IsServer) return;
        Phase.Value = newPhase;
    }
}