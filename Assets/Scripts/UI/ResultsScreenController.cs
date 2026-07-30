using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsScreenController : MonoBehaviour
{
    [SerializeField] private Transform resultsListContainer;
    [SerializeField] private GameObject resultRowPrefab; // строка: "Роль: Score% -> +Валюта"
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private TMP_Text totalCurrencyText;

    private int lastShownCount = -1;

    void OnEnable()
    {
        returnToLobbyButton.onClick.AddListener(OnReturnClicked);
        lastShownCount = -1;
    }

    void OnDisable()
    {
        returnToLobbyButton.onClick.RemoveListener(OnReturnClicked);
    }

    void Update()
    {
        if (GameSessionState.Instance == null) return;

        returnToLobbyButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);

        if (GameSessionState.Instance.Results.Count != lastShownCount)
        {
            Rebuild();
            lastShownCount = GameSessionState.Instance.Results.Count;
        }
    }

    void Rebuild()
    {
        foreach (Transform child in resultsListContainer)
            Destroy(child.gameObject);

        int totalCurrency = 0;

        foreach (var result in GameSessionState.Instance.Results)
        {
            totalCurrency += result.CurrencyEarned;

            var row = Instantiate(resultRowPrefab, resultsListContainer);
            var text = row.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                string scoreSuffix = "";
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(result.ClientId, out var client)
                    && client.PlayerObject != null)
                {
                    var playerScore = client.PlayerObject.GetComponent<PlayerScore>();
                    if (playerScore != null)
                        scoreSuffix = $" | Очков: {playerScore.TotalScore.Value}";
                }

                text.text = $"{result.Role}: {result.Score * 100f:F0}% -> +{result.CurrencyEarned}{scoreSuffix}";
            }
        }

        if (totalCurrencyText != null)
            totalCurrencyText.text = $"Итого валюты: {totalCurrency}";

        // новое — босс не участвует в Results (у него нет головоломки/валюты),
        // но очки под новой системой у него тоже есть — добавляем отдельной строкой
        foreach (var player in LobbyPlayerManager.Instance.Players)
        {
            if (RoleAssignmentManager.Instance.GetRoleFor(player.ClientId) != GameRole.Boss) continue;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(player.ClientId, out var bossClient)
                && bossClient.PlayerObject != null)
            {
                var bossScore = bossClient.PlayerObject.GetComponent<PlayerScore>();
                if (bossScore != null)
                {
                    var row = Instantiate(resultRowPrefab, resultsListContainer);
                    var text = row.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                        text.text = $"Boss: Очков: {bossScore.TotalScore.Value}";
                }
            }
            break;
        }
    }

    void OnReturnClicked()
    {
        GameSessionState.Instance.ReturnToLobbyServerRpc();
    }
}