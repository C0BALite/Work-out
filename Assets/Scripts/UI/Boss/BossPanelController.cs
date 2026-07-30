using TMPro;
using UnityEngine;

// Плейсхолдеры "камер" игроков слева на экране босса — пока просто подпись роль+имя,
// без видео. Обновляется каждый кадр по данным LobbyPlayerManager/RoleAssignmentManager.
public class BossPanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text artistLabel;
    [SerializeField] private TMP_Text programmerLabel;
    [SerializeField] private TMP_Text typographerLabel;

    void Update()
    {
        UpdateLabel(artistLabel, GameRole.Artist, "Художник");
        UpdateLabel(programmerLabel, GameRole.Programmer, "Маркетолог");
        UpdateLabel(typographerLabel, GameRole.Typographer, "Копирайтер");
    }

    void UpdateLabel(TMP_Text label, GameRole role, string roleName)
    {
        if (label == null || LobbyPlayerManager.Instance == null || RoleAssignmentManager.Instance == null) return;

        foreach (var player in LobbyPlayerManager.Instance.Players)
        {
            if (RoleAssignmentManager.Instance.GetRoleFor(player.ClientId) == role)
            {
                label.text = $"{roleName}\n{player.PlayerName}";
                return;
            }
        }

        label.text = $"{roleName}\n—";
    }
}
