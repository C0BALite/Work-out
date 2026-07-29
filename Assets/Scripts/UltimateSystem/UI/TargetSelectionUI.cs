using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TargetSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject targetButtonPrefab; // Button + TMP_Text внутри

    private System.Action<ulong> onTargetChosen;

    void Awake()
    {
        panel.SetActive(false);
    }

    public void Show(System.Action<ulong> callback)
    {
        onTargetChosen = callback;
        Rebuild();
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    void Rebuild()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        ulong myId = NetworkManager.Singleton.LocalClientId;

        foreach (var player in LobbyPlayerManager.Instance.Players)
        {
            if (player.ClientId == myId) continue; // нельзя выбрать себя

            var role = RoleAssignmentManager.Instance.GetRoleFor(player.ClientId);
            var btnObj = Instantiate(targetButtonPrefab, buttonContainer);

            var text = btnObj.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = $"{player.PlayerName} [{role}]";

            ulong targetId = player.ClientId; // локальная копия для замыкания
            btnObj.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                onTargetChosen?.Invoke(targetId);
                Hide();
            });
        }
    }
}