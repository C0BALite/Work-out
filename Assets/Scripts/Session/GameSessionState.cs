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

    public NetworkList<PuzzleResultData> Results; // новое

    [SerializeField] private float roundDuration = 120f;
    public float RoundDuration => roundDuration; // новое

    void Awake()
    {
        Instance = this;
        Results = new NetworkList<PuzzleResultData>(); // новое
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

    // новое
    public void RegisterResult(GameRole role, float score, float timeLeftRatio, ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < Results.Count; i++)
            if (Results[i].ClientId == clientId) return;

        float multiplier = ScoringUtils.ComputeMultiplier(timeLeftRatio, score);
        int currency = Mathf.RoundToInt(100f * multiplier);

        Results.Add(new PuzzleResultData
        {
            ClientId = clientId,
            Role = role,
            Score = score,
            Multiplier = multiplier,
            CurrencyEarned = currency
        });
    }

    // новое
    [ServerRpc(RequireOwnership = false)]
    public void ReturnToLobbyServerRpc()
    {
        Results.Clear();
        LobbyPlayerManager.Instance.ResetAllReady();
        SetPhase(SessionPhase.Lobby);
    }
}