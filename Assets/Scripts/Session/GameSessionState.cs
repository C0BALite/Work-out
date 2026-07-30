using System.Collections.Generic;
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

    // новое — очки за правильные действия в текущем раунде, по клиенту.
    // Только для подсчёта среднего бонуса боссу в конце раунда, сами +10 начисляются сразу.
    private readonly Dictionary<ulong, int> _correctActionsThisRound = new Dictionary<ulong, int>();

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

            if (IsServer && newPhase == SessionPhase.Results) // новое — система очков
            {
                AwardRoundCompletionScores();
            }
        };
    }

    // новое — система очков (отдельно от системы "валюты"/Results.CurrencyEarned выше)
    public void RegisterCorrectAction(ulong clientId)
    {
        if (!IsServer) return;

        _correctActionsThisRound.TryGetValue(clientId, out int count);
        _correctActionsThisRound[clientId] = count + 1;

        TryGetPlayerScore(clientId, out var score);
        score?.AddScore(10);
    }

    private void AwardRoundCompletionScores()
    {
        ulong bossId = 0;
        bool bossFound = false;

        foreach (var player in LobbyPlayerManager.Instance.Players)
        {
            TryGetPlayerScore(player.ClientId, out var score);
            score?.AddScore(200); // за завершение раунда — всем, включая босса

            if (RoleAssignmentManager.Instance.GetRoleFor(player.ClientId) == GameRole.Boss)
            {
                bossId = player.ClientId;
                bossFound = true;
            }
        }

        if (bossFound)
        {
            int bonusSum = 0;
            int bonusCount = 0;
            foreach (var kvp in _correctActionsThisRound)
            {
                if (kvp.Key == bossId) continue; // босс не считается в среднем сам с собой
                bonusSum += kvp.Value * 10;
                bonusCount++;
            }

            if (bonusCount > 0)
            {
                int bossBonus = Mathf.RoundToInt((float)bonusSum / bonusCount);
                TryGetPlayerScore(bossId, out var bossScore);
                bossScore?.AddScore(bossBonus);
            }
        }

        _correctActionsThisRound.Clear();
    }

    private bool TryGetPlayerScore(ulong clientId, out PlayerScore score)
    {
        score = null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return false;
        score = client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerScore>() : null;
        return score != null;
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