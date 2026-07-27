using UnityEngine;
using UnityEngine.UI;

public class TargetSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button designerBtn;
    [SerializeField] private Button marketerBtn;
    [SerializeField] private Button copywriterBtn;

    private System.Action<PlayerRole> callback;

    void Awake()
    {
        designerBtn.onClick.AddListener(() => Select(PlayerRole.Designer));
        marketerBtn.onClick.AddListener(() => Select(PlayerRole.Marketer));
        copywriterBtn.onClick.AddListener(() => Select(PlayerRole.Copywriter));
        panel.SetActive(false);
    }

    public void ShowRoleSelection(System.Action<PlayerRole> onSelected)
    {
        callback = onSelected;
        panel.SetActive(true);
    }

    void Select(PlayerRole role)
    {
        callback?.Invoke(role);
        Hide();
    }

    public void Hide() => panel.SetActive(false);
}