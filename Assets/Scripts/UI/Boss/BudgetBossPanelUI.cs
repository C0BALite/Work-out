using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Первый экран босса: показывает четыре риски программиста, правильную цель,
// текущее положение главной шкалы и сетевой статус выполнения.
public class BudgetBossPanelUI : MonoBehaviour
{
    [SerializeField] private Image budgetFill;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private Button confirmButton;

    [Header("Цвета")]
    [SerializeField] private Color ordinaryMarkerColor = new Color(0.85f, 0.88f, 0.95f, 1f);
    [SerializeField] private Color targetMarkerColor = new Color(0.15f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color currentPositionColor = new Color(1f, 0.2f, 0.25f, 1f);

    private readonly RectTransform[] markers = new RectTransform[4];
    private RectTransform currentPosition;
    private TMP_Text statusLabel;
    private Image statusBackground;

    private void Awake()
    {
        EnsureVisuals();
        SetStatus("Ожидание программиста", false);
    }

    private void Update()
    {
        EnsureVisuals();

        BudgetBossSync sync = BudgetBossSync.Instance;
        if (sync == null || !sync.HasState.Value)
        {
            SetMarkersVisible(false);
            SetStatus("Ожидание программиста", false);
            return;
        }

        SetMarkersVisible(true);
        for (int i = 0; i < markers.Length; i++)
        {
            PositionAt(markers[i], sync.GetMarker(i), 6f, 8f);
            Image image = markers[i].GetComponent<Image>();
            if (image != null)
                image.color = i == sync.TargetMarkerIndex.Value
                    ? targetMarkerColor
                    : ordinaryMarkerColor;
        }

        float denominator = sync.MaxPossibleBudget.Value * 1.1f;
        float normalizedCurrent = denominator > 0f
            ? Mathf.Clamp01(sync.CurrentBudget.Value / denominator)
            : 0f;

        PositionAt(currentPosition, normalizedCurrent, 10f, 22f);
        currentPosition.gameObject.SetActive(true);

        if (budgetFill != null)
            budgetFill.fillAmount = normalizedCurrent;

        SetStatus(sync.Completed.Value ? "Выполнено" : "Не выполнено", sync.Completed.Value);
    }

    private void EnsureVisuals()
    {
        if (targetZone == null || markers[0] != null) return;

        markers[0] = targetZone;
        targetZone.name = "BossMarker_1";
        for (int i = 1; i < markers.Length; i++)
        {
            GameObject markerObject = Instantiate(targetZone.gameObject, targetZone.parent);
            markerObject.name = $"BossMarker_{i + 1}";
            markers[i] = markerObject.GetComponent<RectTransform>();
        }

        GameObject currentObject = Instantiate(targetZone.gameObject, targetZone.parent);
        currentObject.name = "ProgrammerCurrentPosition";
        currentPosition = currentObject.GetComponent<RectTransform>();
        Image currentImage = currentObject.GetComponent<Image>();
        if (currentImage != null)
            currentImage.color = currentPositionColor;
        currentObject.transform.SetAsLastSibling();

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            statusLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
            statusBackground = confirmButton.GetComponent<Image>();
        }
    }

    private static void PositionAt(RectTransform rect, float normalizedX, float width, float extraHeight)
    {
        if (rect == null) return;

        float x = Mathf.Clamp01(normalizedX);
        rect.anchorMin = new Vector2(x, 0f);
        rect.anchorMax = new Vector2(x, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, extraHeight);
    }

    private void SetMarkersVisible(bool visible)
    {
        foreach (RectTransform marker in markers)
            if (marker != null) marker.gameObject.SetActive(visible);

        if (currentPosition != null)
            currentPosition.gameObject.SetActive(visible);
    }

    private void SetStatus(string text, bool completed)
    {
        if (statusLabel != null)
            statusLabel.text = text;

        if (statusBackground != null)
            statusBackground.color = completed
                ? new Color(0.15f, 0.75f, 0.28f, 1f)
                : new Color(0.25f, 0.28f, 0.36f, 1f);
    }
}
