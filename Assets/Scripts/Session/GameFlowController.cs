using UnityEngine;
using TMPro;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameObject lobbyCanvas;
    [SerializeField] private GameObject inGameCanvas;
    [SerializeField] private GameObject resultsCanvas;
    [SerializeField] private GameObject bossCanvas; // новое
    [SerializeField] private TMP_Text timerText;

    private SessionPhase lastAppliedPhase = (SessionPhase)(-1); // невалидное значение, чтобы первый Update точно применил фазу

    void Update()
    {
        if (GameSessionState.Instance == null) return; // ждём пока сервер заспавнит [Game State]

        var phase = GameSessionState.Instance.Phase.Value;

        if (phase != lastAppliedPhase)
        {
            ApplyPhase(phase);
            lastAppliedPhase = phase;
        }

        if (phase == SessionPhase.InGame && timerText != null)
        {
            int seconds = Mathf.CeilToInt(GameSessionState.Instance.TimeRemaining.Value);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        // новое — отдельно от кэша lastAppliedPhase: роль может стать известна позже,
        // чем сама фаза (та же гонка, что уже чинили для головоломок), поэтому пересчитываем каждый кадр
        if (bossCanvas != null)
        {
            bool amBoss = RoleAssignmentManager.Instance != null
                && RoleAssignmentManager.Instance.GetMyRole() == GameRole.Boss;
            bossCanvas.SetActive(phase == SessionPhase.InGame && amBoss);
        }
    }

    void ApplyPhase(SessionPhase phase)
    {
        lobbyCanvas.SetActive(phase == SessionPhase.Lobby || phase == SessionPhase.RoleSelect);
        inGameCanvas.SetActive(phase == SessionPhase.InGame);
        resultsCanvas.SetActive(phase == SessionPhase.Results);
    }
}