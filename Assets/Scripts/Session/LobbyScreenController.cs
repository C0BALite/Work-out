using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyScreenController : MonoBehaviour
{
    [SerializeField] private Transform playerListContainer; // вертикальный список сверху слева
    [SerializeField] private GameObject playerTilePrefab;   // префаб одного "квадратика"
    [SerializeField] private Button actionButton;           // готов / начать
    [SerializeField] private TMP_Text actionButtonLabel;
    [SerializeField] private GameObject settingsPanel;       // сверху справа
    [SerializeField] private Button typographerButton;
    [SerializeField] private Button artistButton;
    [SerializeField] private Button programmerButton;
    [SerializeField] private TMP_Text myRoleLabel;

    private bool localIsReady = false;

    void OnEnable()
    {
        StartCoroutine(WaitForLobbyManagerAndInit());
    }

    System.Collections.IEnumerator WaitForLobbyManagerAndInit()
    {
        while (LobbyPlayerManager.Instance == null || RoleAssignmentManager.Instance == null)
            yield return null;

        LobbyPlayerManager.Instance.Players.OnListChanged += OnPlayersChanged;
        RoleAssignmentManager.Instance.Assignments.OnListChanged += OnRolesChanged; // новое

        actionButton.onClick.AddListener(OnActionButtonClicked);
        typographerButton.onClick.AddListener(() => RoleAssignmentManager.Instance.RequestRoleServerRpc(GameRole.Typographer));
        artistButton.onClick.AddListener(() => RoleAssignmentManager.Instance.RequestRoleServerRpc(GameRole.Artist));
        programmerButton.onClick.AddListener(() => RoleAssignmentManager.Instance.RequestRoleServerRpc(GameRole.Programmer));

        RefreshUI();
        Debug.Log("[Client] Subscribed, IsHost=" + NetworkManager.Singleton.IsHost);
    }

    void OnDisable()
    {
        if (LobbyPlayerManager.Instance != null)
            LobbyPlayerManager.Instance.Players.OnListChanged -= OnPlayersChanged;
        if (RoleAssignmentManager.Instance != null)
            RoleAssignmentManager.Instance.Assignments.OnListChanged -= OnRolesChanged;
    }

    void OnPlayersChanged(NetworkListEvent<LobbyPlayerData> change)
    {
        Debug.Log("[Client] OnPlayersChanged fired");
        RefreshUI();
    }
    void OnActionButtonClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            LobbyPlayerManager.Instance.StartGameServerRpc();
        }
        else
        {
            localIsReady = !localIsReady;
            LobbyPlayerManager.Instance.SetReadyServerRpc(localIsReady);
            RefreshUI();
        }
    }
    void OnRolesChanged(NetworkListEvent<PlayerRoleData> change)
    {
        Debug.Log("[Client] OnRolesChanged fired");
        RefreshUI();
    }
    void RefreshUI()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        var players = LobbyPlayerManager.Instance.Players;
        foreach (var p in players)
        {
            var role = RoleAssignmentManager.Instance.GetRoleFor(p.ClientId);
            var tile = Instantiate(playerTilePrefab, playerListContainer);
            var label = tile.GetComponentInChildren<TMP_Text>();
            label.text = $"{p.PlayerName} [{role}] {(p.IsReady ? "v" : "x")}";
        }

        bool isHost = NetworkManager.Singleton.IsHost;
        var myRole = RoleAssignmentManager.Instance.GetMyRole();

        if (isHost)
        {
            actionButtonLabel.text = "Начать";
            actionButton.interactable = LobbyPlayerManager.Instance.AllNonHostPlayersReady();
            myRoleLabel.text = "Роль: Boss";

            // хосту кнопки выбора роли не нужны — скрываем
            typographerButton.gameObject.SetActive(false);
            artistButton.gameObject.SetActive(false);
            programmerButton.gameObject.SetActive(false);
        }
        else
        {
            myRoleLabel.text = myRole == GameRole.None ? "Роль: не выбрана" : $"Роль: {myRole}";

            // блокируем "Готов", если роль не выбрана
            bool hasRole = myRole != GameRole.None;
            actionButtonLabel.text = localIsReady ? "Не готов" : "Готов";
            actionButton.interactable = hasRole;

            // блокируем уже занятые кем-то другим роли
            typographerButton.interactable = myRole == GameRole.Typographer || !RoleAssignmentManager.Instance.IsRoleTaken(GameRole.Typographer);
            artistButton.interactable = myRole == GameRole.Artist || !RoleAssignmentManager.Instance.IsRoleTaken(GameRole.Artist);
            programmerButton.interactable = myRole == GameRole.Programmer || !RoleAssignmentManager.Instance.IsRoleTaken(GameRole.Programmer);
        }
    }

    public void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}