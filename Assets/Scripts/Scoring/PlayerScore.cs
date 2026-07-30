using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using TMPro;

// Живёт на [Player Ability] у каждого игрока. Текущее значение — сетевая переменная
// (нужно всем видеть на экране результатов), но хранится оно постоянно на ДИСКЕ
// именно того игрока, которому принадлежит — переживает выход и перезапуск игры.
public class PlayerScore : NetworkBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject scoreCanvas; // отдельный canvas, не связан с abilityCanvas

    public NetworkVariable<int> TotalScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool initialScoreReported;

    public override void OnNetworkSpawn()
    {
        // виджет виден только владельцу — иначе каждый клиент видел бы наложенные
        // виджеты очков от всех остальных заспавненных игроков разом
        if (scoreCanvas != null)
            scoreCanvas.SetActive(IsOwner);

        TotalScore.OnValueChanged += OnTotalScoreChanged;
        UpdateUI();

        if (IsOwner)
        {
            int savedScore = PlayerScoreStorage.LoadScore(AuthenticationService.Instance.PlayerId);
            ReportInitialScoreServerRpc(savedScore);
        }
    }

    public override void OnNetworkDespawn()
    {
        TotalScore.OnValueChanged -= OnTotalScoreChanged;
    }

    [ServerRpc]
    private void ReportInitialScoreServerRpc(int savedScore)
    {
        if (initialScoreReported) return;
        initialScoreReported = true;
        TotalScore.Value = savedScore;
    }

    // вызывается только сервером — из GameSessionState/MiniGameEventSystem
    public void AddScore(int delta)
    {
        if (!IsServer) return;
        TotalScore.Value += delta;
    }

    private void OnTotalScoreChanged(int previousValue, int newValue)
    {
        UpdateUI();

        if (IsOwner)
            PlayerScoreStorage.SaveScore(AuthenticationService.Instance.PlayerId, newValue);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Очки: {TotalScore.Value}";
    }
}
