using UnityEngine;
using TMPro;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameObject lobbyCanvas;
    [SerializeField] private GameObject inGameCanvas;
    [SerializeField] private GameObject resultsCanvas;
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
    }

    void ApplyPhase(SessionPhase phase)
    {
        lobbyCanvas.SetActive(phase == SessionPhase.Lobby || phase == SessionPhase.RoleSelect);
        inGameCanvas.SetActive(phase == SessionPhase.InGame);
        resultsCanvas.SetActive(phase == SessionPhase.Results);
    }
}