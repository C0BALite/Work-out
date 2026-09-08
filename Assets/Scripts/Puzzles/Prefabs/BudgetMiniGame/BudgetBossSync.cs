using Unity.Netcode;
using UnityEngine;

// Сетевой мост между головоломкой программиста и первым экраном босса.
public class BudgetBossSync : NetworkBehaviour
{
    public static BudgetBossSync Instance { get; private set; }

    public NetworkVariable<float> CurrentBudget = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetMin = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetMax = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> MaxPossibleBudget = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> Marker0 = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Marker1 = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Marker2 = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Marker3 = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> TargetMarkerIndex = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> HasState = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> Completed = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool rewardGranted;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportStateServerRpc(
        float current,
        float min,
        float max,
        float maxPossible,
        float marker0,
        float marker1,
        float marker2,
        float marker3,
        int targetIndex,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (RoleAssignmentManager.Instance == null ||
            RoleAssignmentManager.Instance.GetRoleFor(senderId) != GameRole.Programmer)
            return;

        CurrentBudget.Value = current;
        TargetMin.Value = min;
        TargetMax.Value = max;
        MaxPossibleBudget.Value = maxPossible;
        Marker0.Value = Mathf.Clamp01(marker0);
        Marker1.Value = Mathf.Clamp01(marker1);
        Marker2.Value = Mathf.Clamp01(marker2);
        Marker3.Value = Mathf.Clamp01(marker3);
        TargetMarkerIndex.Value = Mathf.Clamp(targetIndex, 0, 3);
        HasState.Value = true;

        bool isInTarget = current >= min && current <= max;
        Completed.Value = isInTarget;

        // Статус может снова стать невыполненным, если игрок увёл ползунок.
        // Награду при этом выдаём только за первое попадание в текущем раунде.
        if (isInTarget && !rewardGranted)
        {
            rewardGranted = true;
            if (MiniGameEventSystem.Instance != null)
                MiniGameEventSystem.Instance.ReportCorrectAction(senderId);
        }
    }

    public float GetMarker(int index)
    {
        switch (index)
        {
            case 0: return Marker0.Value;
            case 1: return Marker1.Value;
            case 2: return Marker2.Value;
            case 3: return Marker3.Value;
            default: return 0f;
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }
}
