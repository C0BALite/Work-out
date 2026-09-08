using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Приводит BossCanvas к четырём равноразмерным экранам 2x2.
public class BossDashboardLayout : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.08f, 0.12f, 0.23f, 0.96f);
    private static readonly Color BorderColor = new Color(0.18f, 0.3f, 0.55f, 1f);

    private void Awake()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform leftCameras = transform.Find("LeftCameras");
        if (leftCameras != null)
            leftCameras.gameObject.SetActive(false);

        Transform panelsRoot = transform.Find("RightPanels");
        if (panelsRoot == null) return;

        RectTransform panelsRect = panelsRoot as RectTransform;
        panelsRect.anchorMin = Vector2.zero;
        panelsRect.anchorMax = Vector2.one;
        panelsRect.pivot = new Vector2(0.5f, 0.5f);
        panelsRect.anchoredPosition = Vector2.zero;
        panelsRect.sizeDelta = Vector2.zero;

        Transform programmer = panelsRoot.Find("BudgetPanel");
        Transform artist = panelsRoot.Find("ArtistPlaceholderPanel");
        Transform copywriter = panelsRoot.Find("CopywriterPanel");
        Transform fourth = panelsRoot.Find("ReservePanel");
        if (fourth == null)
            fourth = CreateReservePanel(panelsRoot);

        ConfigureCell(programmer as RectTransform, 0, 1);
        ConfigureCell(artist as RectTransform, 1, 1);
        ConfigureCell(copywriter as RectTransform, 0, 0);
        ConfigureCell(fourth as RectTransform, 1, 0);

        AddBackground(programmer);
        AddBackground(artist);
        AddBackground(copywriter);

        ConvertRootLabelToChild(artist);
        ConfigureProgrammerPanel(programmer);
        ConfigureCopywriterPanel(copywriter);
    }

    private static void ConfigureCell(RectTransform rect, int column, int row)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(column * 0.5f, row * 0.5f);
        rect.anchorMax = new Vector2((column + 1) * 0.5f, (row + 1) * 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(column == 0 ? 24f : 8f, row == 0 ? 24f : 8f);
        rect.offsetMax = new Vector2(column == 0 ? -8f : -24f, row == 0 ? -8f : -24f);
    }

    private static void AddBackground(Transform panel)
    {
        if (panel == null || panel.Find("PanelBackground") != null) return;

        var background = new GameObject("PanelBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(panel, false);
        background.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)background.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = background.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
    }

    private static void ConvertRootLabelToChild(Transform panel)
    {
        if (panel == null || panel.Find("ContentLabel") != null) return;

        TMP_Text source = panel.GetComponent<TMP_Text>();
        if (source == null) return;

        var labelObject = new GameObject("ContentLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel, false);
        RectTransform rect = (RectTransform)labelObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 24f);
        rect.offsetMax = new Vector2(-24f, -24f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = source.text;
        label.font = source.font;
        label.fontSize = 28f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        source.enabled = false;
    }

    private static Transform CreateReservePanel(Transform parent)
    {
        var panelObject = new GameObject("ReservePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = false;

        var labelObject = new GameObject("ReserveLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panelObject.transform, false);
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 24f);
        labelRect.offsetMax = new Vector2(-24f, -24f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        TMP_Text fontSource = parent.GetComponentInChildren<TMP_Text>(true);
        if (fontSource != null) label.font = fontSource.font;
        label.text = "Экран 4\n—";
        label.fontSize = 28f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return panelObject.transform;
    }

    private static void ConfigureProgrammerPanel(Transform panel)
    {
        if (panel == null) return;

        Transform bar = panel.Find("BudgetBar");
        if (bar is RectTransform barRect)
        {
            barRect.anchorMin = new Vector2(0f, 0.5f);
            barRect.anchorMax = new Vector2(1f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(-72f, 28f);
        }

        if (panel.Find("ProgrammerTitle") != null) return;
        var titleObject = new GameObject("ProgrammerTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(panel, false);
        RectTransform titleRect = (RectTransform)titleObject.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(-48f, 48f);

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        TMP_Text fontSource = panel.GetComponentInChildren<TMP_Text>(true);
        if (fontSource != null) title.font = fontSource.font;
        title.text = "ПРОГРАММИСТ";
        title.fontSize = 26f;
        title.fontStyle = FontStyles.Bold;
        title.color = BorderColor;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
    }

    private static void ConfigureCopywriterPanel(Transform panel)
    {
        if (panel == null) return;

        Transform listPanel = panel.Find("DocumentListPanel");
        if (listPanel is RectTransform listRect)
        {
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(18f, 18f);
            listRect.offsetMax = new Vector2(-18f, -70f);
        }

        if (panel.Find("CopywriterTitle") != null) return;
        var titleObject = new GameObject("CopywriterTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(panel, false);
        RectTransform titleRect = (RectTransform)titleObject.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(-48f, 48f);

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        TMP_Text fontSource = panel.GetComponentInChildren<TMP_Text>(true);
        if (fontSource != null) title.font = fontSource.font;
        title.text = "КОПИРАЙТЕР";
        title.fontSize = 26f;
        title.fontStyle = FontStyles.Bold;
        title.color = BorderColor;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
    }
}
