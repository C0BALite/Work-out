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

    private LobbyCodeCopyButton lobbyCodeCopyButton;

#if UNITY_EDITOR
    [Header("Debug (только редактор)")]
    [SerializeField] private Button debugJoinButton; // новое — подключение по коду последнего созданного лобби без ручного ввода, для тестов с несколькими ParrelSync-клонами
#endif

    void Start()
    {
        createButton.onClick.AddListener(OnCreateClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        lobbyCodeCopyButton = lobbyCodeDisplay.GetComponent<LobbyCodeCopyButton>();

#if UNITY_EDITOR
        if (debugJoinButton != null)
            debugJoinButton.onClick.AddListener(OnDebugJoinClicked);
#endif
    }

    async void OnCreateClicked()
    {
        statusText.text = "Creating lobby...";
        try
        {
            string code = await SessionManager.Instance.CreateLobbyAsync("Host");
            lobbyCodeCopyButton.SetCode(code);
            statusText.text = "Lobby created. Waiting for players";

            ShowLobbyScreen(); // новое
        }
        catch (System.Exception e)
        {
            statusText.text = $"Error: {e.Message}";
        }
    }

    async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) { statusText.text = "Enter a code"; return; }

        statusText.text = "Connecting...";
        try
        {
            await SessionManager.Instance.JoinLobbyAsync(code);
            statusText.text = "Connected!";

            ShowLobbyScreen(); // новое
        }
        catch (System.Exception e)
        {
            statusText.text = $"Error: {e.Message}";
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
            statusText.text = "No saved code. Create a lobby in one of the windows first";
            return;
        }

        statusText.text = $"Debug Join with code {code}...";
        try
        {
            await SessionManager.Instance.JoinLobbyAsync(code);
            statusText.text = "Connected!";

            ShowLobbyScreen();
        }
        catch (System.Exception e)
        {
            statusText.text = $"Error: {e.Message}";
        }
    }
#endif
}
