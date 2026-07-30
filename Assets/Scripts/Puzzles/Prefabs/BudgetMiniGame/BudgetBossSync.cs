using Unity.Netcode;
using UnityEngine;

// Живёт на том же GameObject, что и BudgetMiniGame/PuzzleActivator (Puzzle_BudgetMiniGame.prefab —
// один общий на сессию NetworkObject, не привязан к конкретному игроку). Мост между маркетологом
// (репортит своё состояние) и боссом (видит его и подтверждает).
public class BudgetBossSync : NetworkBehaviour
{
    public static BudgetBossSync Instance { get; private set; }

    public NetworkVariable<float> CurrentBudget = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetMin = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetMax = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> MaxPossibleBudget = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ulong _marketerClientId;
    private bool _hasMarketer;

    void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportStateServerRpc(float current, float min, float max, float maxPossible, ServerRpcParams rpcParams = default)
    {
        _marketerClientId = rpcParams.Receive.SenderClientId;
        _hasMarketer = true;

        CurrentBudget.Value = current;
        TargetMin.Value = min;
        TargetMax.Value = max;
        MaxPossibleBudget.Value = maxPossible;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConfirmServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (RoleAssignmentManager.Instance.GetRoleFor(senderId) != GameRole.Boss) return;
        if (!_hasMarketer) return;
        if (CurrentBudget.Value < TargetMin.Value || CurrentBudget.Value > TargetMax.Value) return; // защита от гонки/устаревшей кнопки

        MiniGameEventSystem.Instance.ReportCorrectAction(_marketerClientId);

        // сразу инвалидируем, чтобы кнопка "Подтвердить" не осталась активной,
        // пока не придёт отчёт с новыми данными после регенерации
        CurrentBudget.Value = -999999f;

        RegenerateClientRpc();
    }

    [ClientRpc]
    private void RegenerateClientRpc()
    {
        if (RoleAssignmentManager.Instance.GetMyRole() != GameRole.Programmer) return;

        var budgetGame = GetComponent<BudgetMiniGame>();
        if (budgetGame != null)
            budgetGame.RegenerateFromBoss();
    }
}
