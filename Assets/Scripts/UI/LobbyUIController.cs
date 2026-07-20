using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text lobbyCodeDisplay;

    void Start()
    {
        createButton.onClick.AddListener(OnCreateClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    async void OnCreateClicked()
    {
        statusText.text = "Создаём лобби...";
        try
        {
            string code = await SessionManager.Instance.CreateLobbyAsync("Host");
            lobbyCodeDisplay.text = $"Код лобби: {code}";
            statusText.text = "Лобби создано, ждём игроков";
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
        }
        catch (System.Exception e)
        {
            statusText.text = $"Ошибка: {e.Message}";
        }
    }
}