using UnityEngine;
using UnityEngine.UI;

// Копия шкалы бюджета маркетолога на экране босса — читает NetworkVariable из BudgetBossSync,
// не хранит собственное состояние.
public class BudgetBossPanelUI : MonoBehaviour
{
    [SerializeField] private Image budgetFill;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private Button confirmButton;

    void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    void Update()
    {
        if (BudgetBossSync.Instance == null)
        {
            if (confirmButton != null) confirmButton.interactable = false;
            return;
        }

        float current = BudgetBossSync.Instance.CurrentBudget.Value;
        float min = BudgetBossSync.Instance.TargetMin.Value;
        float max = BudgetBossSync.Instance.TargetMax.Value;
        float denom = BudgetBossSync.Instance.MaxPossibleBudget.Value * 1.1f;

        if (budgetFill != null && denom > 0f)
            budgetFill.fillAmount = Mathf.Clamp01(current / denom);

        PositionZone(min, max, denom);

        if (confirmButton != null)
            confirmButton.interactable = current >= min && current <= max;
    }

    void PositionZone(float min, float max, float denom)
    {
        if (barRect == null || targetZone == null || denom <= 0f) return;
        float barWidth = barRect.rect.width;
        if (barWidth <= 0) return;

        float xMin = (min / denom) * barWidth;
        float zoneWidth = ((max - min) / denom) * barWidth;

        targetZone.anchoredPosition = new Vector2(xMin, 0);
        targetZone.sizeDelta = new Vector2(Mathf.Max(zoneWidth, 4f), barRect.rect.height);
    }

    void OnConfirmClicked()
    {
        BudgetBossSync.Instance?.ConfirmServerRpc();
    }
}
