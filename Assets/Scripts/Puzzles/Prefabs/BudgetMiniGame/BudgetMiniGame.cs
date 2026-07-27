using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BudgetMiniGame : MonoBehaviour
{
    [Serializable]
    public class BudgetSlider
    {
        public Slider slider;
        public TextMeshProUGUI valueText;
        public string label;
    }

    [Header("Ползунки")]
    public BudgetSlider[] sliders = new BudgetSlider[4];

    [Header("Бюджетная шкала")]
    public Slider budgetBar;
    public Image budgetFill;
    public RectTransform targetZone;

    [Header("Параметры")]
    public Color barColor = new Color(0.75f, 0.75f, 0.75f);

    [Header("UI")]
    public TextMeshProUGUI totalText;
    public TextMeshProUGUI statusText;
    [TextArea] public string hintText = "Настройте бюджет, чтобы попасть в зелёную зону...";

    // === ULTIMATE SYSTEM ===
    [Header("Ultimate System")]
    public int playerId = 1; // Маркетолог
    private PlayerState playerState;
    // =======================

    public event Action OnWin;

    private bool hasWon = false;
    private float[] lastValues = new float[4];
    private float[] efficiency = new float[4];
    private float[,] influence = new float[4, 4];
    private float targetMin;
    private float targetMax;
    private float maxPossibleBudget;
    private bool wasInZone = false;

    void Start()
    {
        if (sliders.Length != 4)
        {
            Debug.LogError("Нужно назначить ровно 4 ползунка в инспекторе!");
            return;
        }

        GeneratePuzzle();

        for (int i = 0; i < 4; i++)
        {
            if (sliders[i].slider == null) continue;
            int index = i;
            sliders[i].slider.onValueChanged.AddListener((v) => OnSliderChanged(index, v));
        }

        if (budgetFill != null) budgetFill.color = barColor;
        if (statusText != null) statusText.text = hintText;

        PositionTargetZone();
        UpdateUI();

        // === ULTIMATE SYSTEM ===
        playerState = PlayerManager.Instance?.GetState(playerId);
        // =======================

        Debug.Log($"=== BUDGET PUZZLE === Цель: {targetMin:F0}–{targetMax:F0} | Старт: {GetTotalBudget():F0}");
    }

    void GeneratePuzzle()
    {
        for (int i = 0; i < 4; i++)
            efficiency[i] = UnityEngine.Random.Range(0.7f, 1.3f);

        for (int from = 0; from < 4; from++)
        {
            int[] others = new int[3];
            int idx = 0;
            for (int k = 0; k < 4; k++) if (k != from) others[idx++] = k;

            for (int k = 2; k > 0; k--)
            {
                int j = UnityEngine.Random.Range(0, k + 1);
                (others[k], others[j]) = (others[j], others[k]);
            }

            influence[from, others[0]] = UnityEngine.Random.Range(0.30f, 0.55f);
            influence[from, others[1]] = UnityEngine.Random.Range(-0.55f, -0.30f);
            influence[from, others[2]] = UnityEngine.Random.Range(-0.30f, 0.30f);
        }

        maxPossibleBudget = 0f;
        for (int i = 0; i < 4; i++)
        {
            float max = sliders[i].slider.maxValue;
            if (max <= 0) max = 100f;
            maxPossibleBudget += efficiency[i] * max * 0.6f;
        }

        float targetCenter = UnityEngine.Random.Range(maxPossibleBudget * 0.35f, maxPossibleBudget * 0.55f);
        targetCenter = Mathf.Max(targetCenter, 250f);
        float zoneHalf = UnityEngine.Random.Range(5f, 10f);
        targetMin = targetCenter - zoneHalf;
        targetMax = targetCenter + zoneHalf;

        for (int i = 0; i < 4; i++)
        {
            if (sliders[i].slider == null) continue;
            sliders[i].slider.SetValueWithoutNotify(sliders[i].slider.minValue);
            lastValues[i] = sliders[i].slider.minValue;
        }
    }

    void OnSliderChanged(int changedIndex, float newValue)
    {
        if (hasWon) return;

        float delta = newValue - lastValues[changedIndex];
        if (Mathf.Abs(delta) < 0.01f) return;

        float oldBudget = CalculateBudgetFromValues(lastValues);

        float[] proposed = new float[4];
        for (int i = 0; i < 4; i++) proposed[i] = lastValues[i];
        proposed[changedIndex] = newValue;

        for (int i = 0; i < 4; i++)
        {
            if (i == changedIndex) continue;
            float change = delta * influence[changedIndex, i];
            if (Mathf.Abs(change) < 0.01f) continue;
            proposed[i] = Mathf.Clamp(lastValues[i] + change, sliders[i].slider.minValue, sliders[i].slider.maxValue);
        }

        bool allRight = true;
        bool allLeft = true;
        bool anyMoved = false;
        for (int i = 0; i < 4; i++)
        {
            float d = proposed[i] - lastValues[i];
            if (Mathf.Abs(d) > 0.01f)
            {
                anyMoved = true;
                if (d < 0) allRight = false;
                if (d > 0) allLeft = false;
            }
        }

        // === ВОТ ЭТА СТРОКА БЫЛА ПРОПУЩЕНА ===
        float newBudget = CalculateBudgetFromValues(proposed);
        // =======================================

        bool applyLinks = true;
        if (anyMoved)
        {
            if (allRight && newBudget < oldBudget - 0.01f) applyLinks = false;
            if (allLeft && newBudget > oldBudget + 0.01f) applyLinks = false;
        }

        if (applyLinks)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i == changedIndex) continue;
                if (Mathf.Abs(proposed[i] - lastValues[i]) < 0.01f) continue;
                sliders[i].slider.SetValueWithoutNotify(proposed[i]);
                lastValues[i] = proposed[i];
            }
        }

        lastValues[changedIndex] = newValue;

        UpdateUI();
    }

    void UpdateUI()
    {
        float total = GetTotalBudget();
        for (int i = 0; i < 4; i++)
        {
            if (sliders[i].slider == null) continue;
            if (sliders[i].valueText != null)
                sliders[i].valueText.text = $"{sliders[i].label}: {sliders[i].slider.value:F0}";
        }

        if (budgetBar != null)
            budgetBar.value = Mathf.Clamp01(total / (maxPossibleBudget * 1.1f));

        if (totalText != null)
            totalText.text = $"БЮДЖЕТ: {total:F0}";

        bool nowInZone = IsInTargetZone();
        if (nowInZone && !wasInZone)
        {
            Debug.Log($"[BudgetGame] ✅ Бюджет {total:F0} попал в зелёную зону {targetMin:F0}–{targetMax:F0}!");
        }
        wasInZone = nowInZone;
    }

    float CalculateBudgetFromValues(float[] values)
    {
        float total = 0f;
        for (int i = 0; i < 4; i++)
        {
            float v = values[i];
            float max = sliders[i].slider.maxValue;
            if (max <= 0) continue;
            float t = v / max;
            float payoff = 1f - t * 0.8f;
            total += efficiency[i] * v * payoff;
        }
        return total;
    }

    public float GetTotalBudget()
    {
        float[] vals = new float[4];
        for (int i = 0; i < 4; i++)
            if (sliders[i].slider != null) vals[i] = sliders[i].slider.value;
        return Mathf.Max(0f, CalculateBudgetFromValues(vals));
    }

    public bool IsInTargetZone()
    {
        float total = GetTotalBudget();
        return total >= targetMin && total <= targetMax;
    }

    public void TryWin()
    {
        if (hasWon) return;

        if (IsInTargetZone())
        {
            hasWon = true;
            if (statusText != null)
            {
                statusText.text = "🎉 БЮДЖЕТ ИДЕАЛЬНО СБАЛАНСИРОВАН!";
                statusText.color = new Color(0.2f, 0.9f, 0.3f);
            }

            // === ULTIMATE SYSTEM: правильное действие ===
            GameEvents.ReportCorrectAction(playerId);
            // ============================================

            OnWin?.Invoke();
        }
        else
        {
            float total = GetTotalBudget();
            if (statusText != null)
            {
                if (total < targetMin)
                {
                    statusText.text = "❌ НЕДОБОР. Бюджет слишком мал.";
                    statusText.color = new Color(1f, 0.3f, 0.3f);
                }
                else
                {
                    statusText.text = "❌ ПЕРЕРАСХОД. Бюджет превышен.";
                    statusText.color = new Color(1f, 0.2f, 0.2f);
                }
            }
        }
    }

    public void ResetGame()
    {
        hasWon = false;
        wasInZone = false;
        GeneratePuzzle();
        if (budgetFill != null) budgetFill.color = barColor;
        if (statusText != null) statusText.text = hintText;
        PositionTargetZone();
        UpdateUI();
        Debug.Log($"=== NEW PUZZLE === Цель: {targetMin:F0}–{targetMax:F0} | Старт: {GetTotalBudget():F0}");
    }

    void PositionTargetZone()
    {
        if (budgetBar == null || targetZone == null) return;
        RectTransform barRect = budgetBar.GetComponent<RectTransform>();
        float barWidth = barRect.rect.width;
        if (barWidth <= 0) return;

        float xMin = (targetMin / (maxPossibleBudget * 1.1f)) * barWidth;
        float zoneWidth = ((targetMax - targetMin) / (maxPossibleBudget * 1.1f)) * barWidth;

        targetZone.anchoredPosition = new Vector2(xMin, 0);
        targetZone.sizeDelta = new Vector2(Mathf.Max(zoneWidth, 4f), barRect.rect.height);
    }
}