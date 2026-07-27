using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BudgetMinigame : MonoBehaviour
{
    [Header("Nastroiki mini-igry")]
    public float targetBudget = 500f;
    public float tolerance = 25f;
    public int sliderCount = 5;
    public float minMultiplier = 2f;
    public float maxMultiplier = 5f;

    [Header("Zavisimosti polzunkov")]
    public List<InfluenceRow> influenceMatrix = new List<InfluenceRow>();
    public float maxInfluence = 4f;
    public float minInfluence = 2f;

    private List<float> hiddenMultipliers = new List<float>();
    private List<CustomSlider> customSliders = new List<CustomSlider>();
    private List<TextMeshProUGUI> sliderValueLabels = new List<TextMeshProUGUI>();
    
    private Image budgetBarFill;
    private TextMeshProUGUI budgetText;
    private TextMeshProUGUI targetText;
    private TextMeshProUGUI resultText;
    
    private float currentBudget = 0f;
    private float maxPossibleBudget;
    private bool gameActive = true;
    private bool isUpdating = false;

    [System.Serializable]
    public class InfluenceRow
    {
        public float[] influences;
    }

    void Start()
    {
        EnsureEventSystem();
        InitInfluenceMatrix();
        SetupUI();
        GenerateHiddenMultipliers();
        CalculateMaxBudget();
        UpdateBudgetDisplay();
    }

    void InitInfluenceMatrix()
    {
        if (influenceMatrix == null || influenceMatrix.Count == 0)
        {
            influenceMatrix = new List<InfluenceRow>();
            
            for (int i = 0; i < sliderCount; i++)
            {
                InfluenceRow row = new InfluenceRow();
                row.influences = new float[sliderCount];
                
                for (int target = 0; target < sliderCount; target++)
                {
                    if (target == i) continue;
                    
                    // Kazhdyy polzunok vliyaet na ostal'nykh s FIKSIROVANNYM znacheniyem
                    // Napravleniye zavisit ot ZNAKA v matritze, a ne ot napravleniya dvizheniya
                    float step = Random.Range(minInfluence, maxInfluence);
                    
                    // 75% veroyatnost' chto vliyaniye OTRITSATEL'NOYE (tolkayet nazad)
                    // No nekotoriye mogut byt' pozitivnymi dlya slozhnosti
                    bool negative = Random.value < 0.75f;
                    
                    if (negative)
                        row.influences[target] = -Mathf.Round(step * 10f) / 10f;
                    else
                        row.influences[target] = Mathf.Round(step * 0.5f * 10f) / 10f; // Pozitivnyye - slabeye
                }
                influenceMatrix.Add(row);
            }
        }
        
        Debug.Log("=== MATRITZA ZAVISIMOSTEY ===");
        for (int i = 0; i < influenceMatrix.Count; i++)
        {
            string line = $"Polzunok {i}: ";
            for (int j = 0; j < influenceMatrix[i].influences.Length; j++)
            {
                if (influenceMatrix[i].influences[j] != 0)
                {
                    string dir = influenceMatrix[i].influences[j] > 0 ? "+" : "";
                    line += $"[{j}]{dir}{influenceMatrix[i].influences[j]:F1} ";
                }
            }
            Debug.Log(line);
        }
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            
            #if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
                es.AddComponent<StandaloneInputModule>();
            #endif
        }
    }

    void SetupUI()
    {
        GameObject canvasGO = new GameObject("BudgetMinigameCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject topPanel = CreatePanel(canvasGO.transform, "TopPanel", 
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f), 
            new Color(0.1f, 0.1f, 0.15f, 0.95f));

        CreateText(topPanel.transform, "Title", "OBSHIY BYUDZHET",
            new Vector2(0, 0.72f), new Vector2(1, 1f), 28, TextAlignmentOptions.Center, Color.white, true);

        targetText = CreateText(topPanel.transform, "TargetText", "",
            new Vector2(0, 0.52f), new Vector2(1, 0.72f), 18, TextAlignmentOptions.Center, new Color(0.4f, 0.9f, 0.4f), false);
        targetText.text = $"Cel: {targetBudget - tolerance:F0} - {targetBudget + tolerance:F0}";

        budgetText = CreateText(topPanel.transform, "BudgetValue", "Tekushiy byudzhet: 0",
            new Vector2(0, 0.32f), new Vector2(1, 0.52f), 20, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.9f), false);

        GameObject barGO = CreatePanel(topPanel.transform, "BudgetBar", 
            new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.28f), 
            new Color(0.15f, 0.15f, 0.2f, 1f));

        GameObject barFillGO = CreatePanel(barGO.transform, "BarFill", 
            new Vector2(0, 0), new Vector2(0, 1), 
            new Color(0.3f, 0.6f, 1f, 1f));
        RectTransform barFillRT = barFillGO.GetComponent<RectTransform>();
        barFillRT.anchorMax = new Vector2(0, 1);
        budgetBarFill = barFillGO.GetComponent<Image>();

        GameObject targetZone = CreatePanel(barGO.transform, "TargetZone",
            new Vector2(Mathf.Clamp01((targetBudget - tolerance) / 1000f), -0.2f),
            new Vector2(Mathf.Clamp01((targetBudget + tolerance) / 1000f), 1.2f),
            new Color(0.2f, 0.8f, 0.3f, 0.4f));

        GameObject midPanel = CreatePanel(canvasGO.transform, "SlidersPanel",
            new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.75f),
            new Color(0.08f, 0.08f, 0.12f, 0.95f));

        CreateText(midPanel.transform, "Title", "RASPREDELENIE BYUDZHETA",
            new Vector2(0, 0.88f), new Vector2(1, 1f), 24, TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 1f), true);

        float usableH = 0.85f;
        float slotH = usableH / sliderCount;
        
        for (int i = 0; i < sliderCount; i++)
        {
            float yMax = 0.88f - (i * slotH);
            float yMin = yMax - slotH + 0.01f;
            CreateSliderRow(midPanel.transform, i, yMin, yMax);
        }

        GameObject botPanel = CreatePanel(canvasGO.transform, "ButtonsPanel",
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.15f),
            new Color(0.05f, 0.05f, 0.08f, 0f));

        CreateButton(botPanel.transform, "Confirm", "PODTVERDIT", 
            new Vector2(0, 0), new Vector2(0.48f, 1), 
            new Color(0.2f, 0.7f, 0.3f, 1f), OnConfirmClicked);

        CreateButton(botPanel.transform, "Reset", "SBROSIT", 
            new Vector2(0.52f, 0), new Vector2(1, 1), 
            new Color(0.7f, 0.3f, 0.2f, 1f), OnResetClicked);

        GameObject resultGO = new GameObject("ResultText");
        resultGO.transform.SetParent(canvasGO.transform, false);
        RectTransform resultRT = resultGO.AddComponent<RectTransform>();
        resultRT.anchorMin = new Vector2(0.2f, 0.4f);
        resultRT.anchorMax = new Vector2(0.8f, 0.6f);
        resultRT.offsetMin = Vector2.zero;
        resultRT.offsetMax = Vector2.zero;
        resultText = resultGO.AddComponent<TextMeshProUGUI>();
        resultText.text = "";
        resultText.fontSize = 32;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = Color.white;
        resultText.fontStyle = FontStyles.Bold;
        resultText.enabled = false;
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAlignmentOptions align, Color color, bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    void CreateSliderRow(Transform parent, int index, float yMin, float yMax)
    {
        GameObject row = new GameObject($"Row_{index}");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.02f, yMin);
        rowRT.anchorMax = new Vector2(0.98f, yMax);
        rowRT.offsetMin = Vector2.zero;
        rowRT.offsetMax = Vector2.zero;

        string[] names = { "SMM", "TV-reklama", "SEO", "Email", "Partnerstva", "Eventy", "PR", "Target" };
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowRT, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.1f);
        labelRT.anchorMax = new Vector2(0.22f, 0.9f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = names[index % names.Length];
        labelTMP.fontSize = 18;
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.color = new Color(0.8f, 0.8f, 0.9f);

        GameObject valGO = new GameObject("Value");
        valGO.transform.SetParent(rowRT, false);
        RectTransform valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.85f, 0.1f);
        valRT.anchorMax = new Vector2(1, 0.9f);
        valRT.offsetMin = Vector2.zero;
        valRT.offsetMax = Vector2.zero;
        TextMeshProUGUI valTMP = valGO.AddComponent<TextMeshProUGUI>();
        valTMP.text = "0%";
        valTMP.fontSize = 18;
        valTMP.alignment = TextAlignmentOptions.Right;
        valTMP.color = new Color(0.6f, 0.8f, 1f);
        sliderValueLabels.Add(valTMP);

        GameObject trackGO = new GameObject("Track");
        trackGO.transform.SetParent(rowRT, false);
        RectTransform trackRT = trackGO.AddComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0.24f, 0.15f);
        trackRT.anchorMax = new Vector2(0.83f, 0.85f);
        trackRT.offsetMin = Vector2.zero;
        trackRT.offsetMax = Vector2.zero;
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(trackRT, false);
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.5f, 0.9f, 1f);

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(trackRT, false);
        RectTransform handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(24, 36);
        handleRT.anchorMin = new Vector2(0, 0.5f);
        handleRT.anchorMax = new Vector2(0, 0.5f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.anchoredPosition = Vector2.zero;
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.7f, 1f, 1f);

        CustomSlider cs = trackGO.AddComponent<CustomSlider>();
        cs.Setup(trackRT, fillRT, handleRT, (v) => OnSliderChanged(index, v));
        customSliders.Add(cs);
    }

    void CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        Image img = go.AddComponent<Image>();
        img.color = color;
        
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
    }

    void GenerateHiddenMultipliers()
    {
        hiddenMultipliers.Clear();
        for (int i = 0; i < sliderCount; i++)
        {
            float mult = Random.Range(minMultiplier, maxMultiplier);
            mult = Mathf.Round(mult * 10f) / 10f;
            hiddenMultipliers.Add(mult);
        }
        Debug.Log("=== SKRYTYYE MNOZHITELI ===");
        for (int i = 0; i < hiddenMultipliers.Count; i++)
            Debug.Log($"Polzunok {i}: x{hiddenMultipliers[i]}");
    }

    void CalculateMaxBudget()
    {
        maxPossibleBudget = 0f;
        foreach (float m in hiddenMultipliers)
            maxPossibleBudget += 100f * m;
    }

    void OnSliderChanged(int changedIndex, float newValue)
    {
        if (!gameActive || isUpdating) return;
        
        float oldValue = customSliders[changedIndex].PreviousValue;
        float delta = newValue - oldValue;
        if (Mathf.Abs(delta) < 0.5f) return;
        
        bool hitLeftWall = (oldValue <= 0.1f && newValue <= 0.1f);
        bool hitRightWall = (oldValue >= 99.9f && newValue >= 99.9f);
        if (hitLeftWall || hitRightWall) return;
        
        isUpdating = true;
        
        if (changedIndex < influenceMatrix.Count)
        {
            for (int targetIndex = 0; targetIndex < sliderCount; targetIndex++)
            {
                if (targetIndex == changedIndex) continue;
                
                float influence = influenceMatrix[changedIndex].influences[targetIndex];
                if (influence != 0)
                {
                    // ISPRAVLENIYE: znak vliyaniya FIKSIROVANNYY iz matritzy
                    // Ne zavisit ot napravleniya dvizheniya!
                    // Naprimer, esli influence = -3, to pri LYUBOM dvizhenii polzunok 0
                    // budet tolkat' polzunok target na -3 (vsegda nazad)
                    float shift = influence;
                    
                    float newVal = customSliders[targetIndex].Value + shift;
                    customSliders[targetIndex].SetValue(newVal);
                    sliderValueLabels[targetIndex].text = $"{customSliders[targetIndex].Value:F0}%";
                }
            }
        }
        
        sliderValueLabels[changedIndex].text = $"{newValue:F0}%";
        
        RecalculateBudget();
        UpdateBudgetDisplay();
        
        isUpdating = false;
    }

    void RecalculateBudget()
    {
        currentBudget = 0f;
        for (int i = 0; i < customSliders.Count; i++)
            currentBudget += customSliders[i].Value * hiddenMultipliers[i];
    }

    void UpdateBudgetDisplay()
    {
        float maxVal = Mathf.Max(1000f, maxPossibleBudget * 1.2f);
        float fillAmount = Mathf.Clamp01(currentBudget / maxVal);
        
        RectTransform rt = budgetBarFill.GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(fillAmount, 1);

        budgetText.text = $"Tekushiy byudzhet: {currentBudget:F0}";

        float dist = Mathf.Abs(currentBudget - targetBudget);
        if (dist <= tolerance)
            budgetBarFill.color = new Color(0.2f, 0.9f, 0.3f, 1f);
        else if (dist <= tolerance * 2.5f)
            budgetBarFill.color = new Color(0.9f, 0.8f, 0.2f, 1f);
        else
            budgetBarFill.color = new Color(0.3f, 0.6f, 1f, 1f);
    }

    void OnConfirmClicked()
    {
        if (!gameActive) return;
        float dist = Mathf.Abs(currentBudget - targetBudget);
        bool success = dist <= tolerance;

        resultText.enabled = true;
        if (success)
        {
            resultText.text = $"OTlichno!\nByudzhet: {currentBudget:F0}\nPopadaniye v tsel!";
            resultText.color = new Color(0.2f, 0.9f, 0.3f, 1f);
        }
        else
        {
            string dir = currentBudget < targetBudget ? "nedobor" : "perebor";
            resultText.text = $"Mimo!\nByudzhet: {currentBudget:F0}\n{dir} na {dist:F0}";
            resultText.color = new Color(0.9f, 0.3f, 0.2f, 1f);
        }
        gameActive = false;
    }

    void OnResetClicked()
    {
        gameActive = true;
        resultText.enabled = false;
        for (int i = 0; i < customSliders.Count; i++)
        {
            customSliders[i].SetValue(0);
            sliderValueLabels[i].text = "0%";
        }
        influenceMatrix.Clear();
        InitInfluenceMatrix();
        GenerateHiddenMultipliers();
        CalculateMaxBudget();
        RecalculateBudget();
        UpdateBudgetDisplay();
    }

    public bool IsSuccess() => Mathf.Abs(currentBudget - targetBudget) <= tolerance;
    public float GetCurrentBudget() => currentBudget;

    private class CustomSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform track;
        private RectTransform fill;
        private RectTransform handle;
        private System.Action<float> onValueChanged;
        
        public float Value { get; private set; } = 0f;
        public float PreviousValue { get; private set; } = 0f;

        public void Setup(RectTransform track, RectTransform fill, RectTransform handle, System.Action<float> callback)
        {
            this.track = track;
            this.fill = fill;
            this.handle = handle;
            this.onValueChanged = callback;
            
            Image img = GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
        }

        public void SetValue(float value)
        {
            PreviousValue = Value;
            Value = Mathf.Clamp(value, 0, 100);
            UpdateVisual();
        }

        void UpdateVisual()
        {
            float t = Value / 100f;
            fill.anchorMax = new Vector2(t, 1);
            handle.anchorMin = new Vector2(t, 0.5f);
            handle.anchorMax = new Vector2(t, 0.5f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateFromPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            UpdateFromPointer(eventData);
        }

        void UpdateFromPointer(PointerEventData eventData)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                track, eventData.position, eventData.pressEventCamera, out localPos);
            
            float width = track.rect.width;
            float t = Mathf.Clamp01((localPos.x + width * 0.5f) / width);
            
            PreviousValue = Value;
            Value = t * 100f;
            UpdateVisual();
            onValueChanged?.Invoke(Value);
        }
    }
}