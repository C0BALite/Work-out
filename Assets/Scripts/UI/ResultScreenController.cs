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
                text.text = $"{result.Role}: {result.Score * 100f:F0}% -> +{result.CurrencyEarned}";
        }

        if (totalCurrencyText != null)
            totalCurrencyText.text = $"Итого валюты: {totalCurrency}";
    }

    void OnReturnClicked()
    {
        GameSessionState.Instance.ReturnToLobbyServerRpc();
    }
}