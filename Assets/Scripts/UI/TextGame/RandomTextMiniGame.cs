using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class BudgetInspector : MonoBehaviour
{
    [Header("Спрайты (назначь в инспекторе)")]
    public Sprite logoSprite;
    public Sprite stampSprite;
    public Sprite photoSprite;
    public Sprite signatureSprite;

    private Canvas canvas;
    private RectTransform docPanel;
    private GameObject feedbackPanel;
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI instructionText;
    private bool waitingForNextDoc;

    private (string request, bool shouldApprove)[] budgetRequests;

    void Start()
    {
        InitRequests();
        SetupCanvas();
        SetupInput();
        CreateFeedbackPanel();
        SpawnDocument();
    }

    void InitRequests()
    {
        budgetRequests = new (string, bool)[]
        {
            ("Закупка 500 золотых ручек для отдела кадров", false),
            ("Покраска офиса в цвет настроения CEO", false),
            ("Покупка 3D-принтера для печати печенья", false),
            ("Аренда вертолета для доставки кофе", false),
            ("Закупка 200 надувных единорогов для коридора", false),
            ("Покупка личного массажиста для степлера", false),
            ("Бюджет на ежемесячный корпоратив в Дубае", false),
            ("Закупка 1000 пакетиков чая 'Для бедных'", true),
            ("Покупка нового принтера (старый горит)", true),
            ("Ремонт кофемашины (все умирают без кофе)", true),
            ("Закупка бумаги A4 для печати документов", true),
            ("Покупка стульев (сотрудники сидят на коробках)", true),
            ("Замена лампочек (работаем в темноте 3 недели)", true),
            ("Закупка туалетной бумаги (критически важно)", true),
            ("Покупка Wi-Fi роутера (интернет через голубей)", true),
            ("Бюджет на обучение 'Как не гореть на работе'", false),
            ("Закупка личного бариста для каждого сотрудника", false),
            ("Покупка золотого унитаза для VIP-туалета", false),
            ("Аренда яхты для 'командообразования'", false),
            ("Закупка 50 гамаков для open-space", false),
        };
    }

    void SetupCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject docGO = new GameObject("Document");
        docGO.transform.SetParent(canvasGO.transform, false);
        docPanel = docGO.AddComponent<RectTransform>();
        docPanel.anchorMin = new Vector2(0.5f, 0.5f);
        docPanel.anchorMax = new Vector2(0.5f, 0.5f);
        docPanel.pivot = new Vector2(0.5f, 0.5f);
        docPanel.sizeDelta = new Vector2(550, 700);
        docPanel.anchoredPosition = Vector2.zero;

        Image bg = docGO.AddComponent<Image>();
        bg.color = new Color(0.98f, 0.98f, 0.96f);

        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(docPanel, false);
        RectTransform borderRect = borderGO.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-4, -4);
        borderRect.offsetMax = new Vector2(4, 4);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0.6f, 0.6f, 0.6f);
        borderImg.raycastTarget = false;

        CreateTexts();
        CreateButtons();
    }

    void SetupInput()
    {
        var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject eventGO = new GameObject("EventSystem");
            es = eventGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventGO.AddComponent<InputSystemUIInputModule>();
        }
    }

    void CreateTexts()
    {
        GameObject instGO = new GameObject("Instruction");
        instGO.transform.SetParent(docPanel, false);
        RectTransform ir = instGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.5f, 0.5f);
        ir.anchorMax = new Vector2(0.5f, 0.5f);
        ir.pivot = new Vector2(0.5f, 0.5f);
        ir.anchoredPosition = new Vector2(0, -370);
        ir.sizeDelta = new Vector2(520, 50);

        instructionText = instGO.AddComponent<TextMeshProUGUI>();
        instructionText.fontSize = 15;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.color = new Color(0.5f, 0.5f, 0.5f);
        instructionText.text = "Ты — бухгалтер. Реши: одобрить расход или отклонить?";
    }

    void CreateButtons()
    {
        float btnY = -300;

        CreateButton("ОДОБРИТЬ", new Vector2(-100, btnY), new Color(0.2f, 0.7f, 0.3f), OnApprove);
        CreateButton("ОТКЛОНИТЬ", new Vector2(100, btnY), new Color(0.85f, 0.2f, 0.2f), OnReject);
    }

    void CreateButton(string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject btn = new GameObject("Btn_" + label);
        btn.transform.SetParent(docPanel, false);
        btn.transform.SetAsLastSibling();

        RectTransform r = btn.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(170, 55);

        Image img = btn.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        Button b = btn.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(action);
        b.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = b.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.5f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.1f;
        b.colors = colors;

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(btn.transform, false);
        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }

    void CreateFeedbackPanel()
    {
        feedbackPanel = new GameObject("FeedbackPanel");
        feedbackPanel.transform.SetParent(canvas.transform, false);
        feedbackPanel.SetActive(false);

        RectTransform fr = feedbackPanel.AddComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.5f, 0.5f);
        fr.anchorMax = new Vector2(0.5f, 0.5f);
        fr.pivot = new Vector2(0.5f, 0.5f);
        fr.anchoredPosition = Vector2.zero;
        fr.sizeDelta = new Vector2(400, 200);

        Image bg = feedbackPanel.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

        GameObject border = new GameObject("Border");
        border.transform.SetParent(feedbackPanel.transform, false);
        RectTransform br = border.AddComponent<RectTransform>();
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = new Vector2(-3, -3);
        br.offsetMax = new Vector2(3, 3);
        Image bi = border.AddComponent<Image>();
        bi.color = new Color(0.5f, 0.5f, 0.5f);
        bi.raycastTarget = false;

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(feedbackPanel.transform, false);
        RectTransform tr = txtGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(20, 20);
        tr.offsetMax = new Vector2(-20, -20);

        feedbackText = txtGO.AddComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 26;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.color = Color.white;
    }

    void SpawnDocument()
    {
        waitingForNextDoc = false;
        ClearDocument();

        var request = budgetRequests[Random.Range(0, budgetRequests.Length)];
        bool shouldApprove = request.shouldApprove;
        docPanel.name = shouldApprove ? "APPROVE" : "REJECT";

        float y = 280;

        AddText("ООО \"БЮРОКРАТИЯ\"", new Vector2(0, y), 22, FontStyles.Bold, new Color(0.1f, 0.1f, 0.4f));
        y -= 55;

        AddImage(logoSprite, new Vector2(-160, y + 10), new Vector2(70, 70));
        y -= 30;

        AddText("ЗАЯВКА НА БЮДЖЕТ", new Vector2(0, y), 18, FontStyles.Bold, Color.black);
        y -= 40;

        AddText("№" + Random.Range(10000, 99999) + " от " + System.DateTime.Now.ToString("dd.MM.yyyy"), new Vector2(0, y), 13, FontStyles.Normal, Color.gray);
        y -= 60;

        AddLine(y);
        y -= 40;

        AddText("ПРОСЬБА ВЫДЕЛИТЬ БЮДЖЕТ:", new Vector2(0, y), 14, FontStyles.Bold, new Color(0.3f, 0.3f, 0.3f));
        y -= 35;

        AddText(request.request, new Vector2(0, y), 20, FontStyles.Bold, Color.black, 480);
        y -= 80;

        int amount = shouldApprove 
            ? Random.Range(500, 5000) 
            : Random.Range(50000, 500000);
        AddText("СУММА: " + amount.ToString("N0") + " ₽", new Vector2(0, y), 18, FontStyles.Bold, new Color(0.7f, 0.1f, 0.1f));
        y -= 50;

        string[] goodReasons = {
            "Обоснование: Критически необходимо для работы.",
            "Обоснование: Без этого отдел остановится.",
            "Обоснование: Закон требует (probably).",
            "Обоснование: Уже 3 месяца терпим."
        };
        string[] badReasons = {
            "Обоснование: Потому что я CEO.",
            "Обоснование: Для повышения морального духа.",
            "Обоснование: Это инвестиция в будущее (maybe).",
            "Обоснование: Видел у конкурентов — завидую."
        };
        string reason = shouldApprove 
            ? goodReasons[Random.Range(0, goodReasons.Length)]
            : badReasons[Random.Range(0, badReasons.Length)];
        AddText(reason, new Vector2(0, y), 13, FontStyles.Italic, new Color(0.4f, 0.4f, 0.4f), 460);
        y -= 80;

        AddLine(y);
        y -= 40;

        // Подпись и печать в ОДНУ СТРОЧКУ по центру, над кнопками
        // Подпись слева от центра, печать справа от центра
        AddText("Подпись:", new Vector2(-60, y), 12, FontStyles.Normal, Color.black);
        AddImage(signatureSprite, new Vector2(10, y), new Vector2(90, 35));
        AddText("Печать:", new Vector2(100, y), 12, FontStyles.Normal, Color.black);
        AddImage(stampSprite, new Vector2(170, y), new Vector2(60, 60));

        // Фото иногда справа
        if (Random.value > 0.5f)
        {
            AddImage(photoSprite, new Vector2(200, 80), new Vector2(100, 80));
        }
    }

    void AddText(string text, Vector2 pos, float size, FontStyles style, Color color, float width = 500)
    {
        GameObject go = new GameObject("Text_" + text.GetHashCode());
        go.transform.SetParent(docPanel, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(width, 30);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        tmp.ForceMeshUpdate();
        r.sizeDelta = new Vector2(width, tmp.preferredHeight + 5);
    }

    void AddImage(Sprite sprite, Vector2 pos, Vector2 size)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("Img_" + sprite.name);
        go.transform.SetParent(docPanel, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
    }

    void AddLine(float y)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(docPanel, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0, y);
        r.sizeDelta = new Vector2(480, 2);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.7f, 0.7f, 0.7f);
    }

    void OnApprove()
    {
        if (waitingForNextDoc) return;

        bool shouldApprove = docPanel.name == "APPROVE";

        if (shouldApprove)
            ShowFeedback("ОДОБРЕНО!\nРазумный расход.", Color.green);
        else
            ShowFeedback("ОДОБРЕНО?!\nТы серьезно? Это же абсурд!", Color.red);

        waitingForNextDoc = true;
        StartCoroutine(NextDoc(2f));
    }

    void OnReject()
    {
        if (waitingForNextDoc) return;

        bool shouldApprove = docPanel.name == "APPROVE";

        if (!shouldApprove)
            ShowFeedback("ОТКЛОНЕНО!\nХороший глаз, босс бы одобрил.", Color.green);
        else
            ShowFeedback("ОТКЛОНЕНО?!\nНо это же нужная вещь!", Color.red);

        waitingForNextDoc = true;
        StartCoroutine(NextDoc(2f));
    }

    void ShowFeedback(string msg, Color color)
    {
        feedbackText.text = msg;
        feedbackText.color = color;
        feedbackPanel.SetActive(true);
        feedbackPanel.transform.SetAsLastSibling();
        StopAllCoroutines();
        StartCoroutine(HideFeedback());
    }

    IEnumerator HideFeedback()
    {
        yield return new WaitForSeconds(2f);
        feedbackPanel.SetActive(false);
    }

    IEnumerator NextDoc(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnDocument();
    }

    void ClearDocument()
    {
        List<Transform> toKeep = new List<Transform>();
        for (int i = 0; i < docPanel.childCount; i++)
        {
            Transform child = docPanel.GetChild(i);
            string name = child.name;
            if (name.StartsWith("Border") || name.StartsWith("Instruction") || name.StartsWith("Btn_"))
                toKeep.Add(child);
        }

        foreach (var t in toKeep)
            t.SetParent(canvas.transform, true);

        for (int i = docPanel.childCount - 1; i >= 0; i--)
            Destroy(docPanel.GetChild(i).gameObject);

        foreach (var t in toKeep)
            t.SetParent(docPanel, true);
    }
}