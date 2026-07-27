using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFlowController : MonoBehaviour
{
    public static UltimateFlowController Instance;

    [SerializeField] private WheelConfig wheelConfig;
    [SerializeField] private DebuffDatabase debuffDatabase;
    [SerializeField] private DebuffWheelUI wheelUI;
    [SerializeField] private TargetSelectionUI targetUI;
    [SerializeField] private HammerModeUI hammerUI;

    private int currentPlayerId;
    private FlowState state = FlowState.Idle;

    enum FlowState { Idle, Spinning, SelectingRole, SelectingHammerTarget, Applying }

    void Awake() => Instance = this;

    public void StartFlow(int playerId)
    {
        if (state != FlowState.Idle) return;
        currentPlayerId = playerId;
        state = FlowState.Spinning;
        wheelUI.Show();
        wheelUI.Spin(wheelConfig, OnWheelStopped);
    }

    void OnWheelStopped(WheelSectorData sector)
    {
        if (sector.type == SectorType.Global)
        {
            if (sector.globalDebuff != null)
            {
                ApplyGlobalDebuff(sector.globalDebuff);
                EndFlow();
            }
            else
            {
                // Если это молоток — проверяем по имени или отдельному флагу
                // Для простоты: молоток — это отдельная логика
                state = FlowState.SelectingHammerTarget;
                wheelUI.Hide();
                EnterHammerMode();
            }
        }
        else
        {
            state = FlowState.SelectingRole;
            wheelUI.Hide();
            targetUI.ShowRoleSelection(OnRoleSelected);
        }
    }

    void OnRoleSelected(PlayerRole role)
    {
        var debuff = debuffDatabase.GetRandomForRole(role);
        var target = PlayerManager.Instance.GetPlayerByRole(role);

        if (debuff != null && target != null)
        {
            debuff.Apply(target.State);
            StartCoroutine(RemoveDebuffLater(debuff, target.State));
        }

        targetUI.Hide();
        EndFlow();
    }

    void EnterHammerMode()
    {
        // Для молотка используем отдельный SO или захардкоженные параметры
        // Здесь упрощённый вариант — можно расширить
        var hammerData = Resources.Load<HammerData>("HammerDebuff"); // или SerializeField
        var others = PlayerManager.Instance.GetOtherPlayers(currentPlayerId);
        hammerUI.Show(others, hammerData, OnHammerModeFinished);
    }

    void OnHammerModeFinished() => EndFlow();

    void ApplyGlobalDebuff(DebuffData debuff)
    {
        foreach (var p in PlayerManager.Instance.AllPlayers)
        {
            if (p.PlayerId == currentPlayerId) continue;
            debuff.Apply(p.State);
            StartCoroutine(RemoveDebuffLater(debuff, p.State));
        }
    }

    IEnumerator RemoveDebuffLater(DebuffData debuff, PlayerState target)
    {
        yield return new WaitForSeconds(debuff.duration);
        debuff.Remove(target);
    }

    void EndFlow()
    {
        state = FlowState.Idle;
        currentPlayerId = -1;
    }
}
