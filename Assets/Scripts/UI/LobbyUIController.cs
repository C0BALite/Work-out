using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text lobbyCodeDisplay;

    [SerializeField] private GameObject createJoinPanel;   // новое поле
    [SerializeField] private GameObject lobbyScreenPanel;  // новое поле

#if UNITY_EDITOR
    [Header("Debug (только редактор)")]
    [SerializeField] private Button debugJoinButton; // новое — подключение по коду последнего созданного лобби без ручного ввода, для тестов с несколькими ParrelSync-клонами
#endif

    void Start()
    {
        createButton.onClick.AddListener(OnCreateClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

#if UNITY_EDITOR
        if (debugJoinButton != null)
            debugJoinButton.onClick.AddListener(OnDebugJoinClicked);
#endif
    }

    async void OnCreateClicked()
    {
        statusText.text = "Создаём лобби...";
        try
        {
            string code = await SessionManager.Instance.CreateLobbyAsync("Host");
            lobbyCodeDisplay.text = $"Код лобби: {code}";
            statusText.text = "Лобби создано, ждём игроков";

            ShowLobbyScreen(); // новое
        }
        catch (System.Exception e)
        {
            statusText.text = $"Ошибка: {e.Message}";
        }
    }

    async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) { statusText.text = "Введите код"; return; }

        statusText.text = "Подключаемся...";
        try
        {
            await SessionManager.Instance.JoinLobbyAsync(code);
            statusText.text = "Подключено!";

            ShowLobbyScreen(); // новое
        }
        catch (System.Exception e)
        {
            statusText.text = $"Ошибка: {e.Message}";
        }
    }

    void ShowLobbyScreen() // новый метод
    {
        createJoinPanel.SetActive(false);
        lobbyScreenPanel.SetActive(true);
    }

#if UNITY_EDITOR
    async void OnDebugJoinClicked()
    {
        string code = SessionManager.DebugReadLastLobbyCode();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Нет сохранённого кода — сначала создайте лобби в одном из окон";
            return;
        }

        statusText.text = $"Debug Join по коду {code}...";
        try
        {
            await SessionManager.Instance.JoinLobbyAsync(code);
            statusText.text = "Подключено!";

            ShowLobbyScreen();
        }
        catch (System.Exception e)
        {
            statusText.text = $"Ошибка: {e.Message}";
        }
    }
#endif
}