using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Постоянно открытый прокручиваемый справочник документов для экрана босса.
public class DocumentReferencePanelUI : MonoBehaviour
{
    [SerializeField] private Button folderButton;
    [SerializeField] private GameObject listPanel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject rowPrefab;

    private const float RowHeight = 70f;
    private bool built;
    [SerializeField] private ScrollRect scrollRect;

    private void Start()
    {
        if (scrollRect == null && listPanel != null) scrollRect = listPanel.GetComponent<ScrollRect>();
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    #if UNITY_EDITOR
    public void BakeLayout()
    {
        if (folderButton != null)
            folderButton.gameObject.SetActive(false);

        if (listPanel == null) return;

        // Раньше список был отдельным всплывающим окном в корне Canvas.
        // Теперь он постоянно занимает панель копирайтера в сетке 2x2.
        listPanel.transform.SetParent(transform, false);
        RectTransform listRect = listPanel.GetComponent<RectTransform>();
        if (listRect != null)
        {
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(18f, 18f);
            listRect.offsetMax = new Vector2(-18f, -70f);
        }

        Image listBackground = listPanel.GetComponent<Image>();
        if (listBackground != null)
        {
            // Сохраняем Image для обработки drag/scroll, но убираем чёрную заливку.
            listBackground.color = Color.clear;
            listBackground.raycastTarget = true;
        }

        listPanel.SetActive(true);
        ConfigureScrolling(listRect);
        Build();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ConfigureScrolling(RectTransform viewport)
    {
        if (viewport == null || rowContainer == null) return;

        RectTransform content = rowContainer as RectTransform;
        if (content == null) return;

        if (listPanel.GetComponent<RectMask2D>() == null)
            listPanel.AddComponent<RectMask2D>();

        scrollRect = listPanel.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = listPanel.AddComponent<ScrollRect>();

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, -8f);
        content.sizeDelta = new Vector2(-16f, DocumentRequestData.All.Length * RowHeight + 16f);

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
    }

    private void Build()
    {
        if (built || rowContainer == null || rowPrefab == null) return;
        built = true;

        int index = 0;
        foreach (var entry in DocumentRequestData.All)
        {
            GameObject row = Instantiate(rowPrefab, rowContainer);
            WorkOutTheme.Skin(row.transform, WorkOutTheme.Surface.Raised).raycastTarget = false;
            RectTransform rowRect = row.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -index * RowHeight);
                rowRect.sizeDelta = new Vector2(0f, RowHeight - 4f);
            }

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                WorkOutTheme.Text(texts[0], 18f);
                WorkOutTheme.Text(texts[1], 16f);
                WorkOutTheme.Place(texts[0].transform, 0f, 0.35f, 1f, 1f, 12f, 0f, 12f, 3f);
                WorkOutTheme.Place(texts[1].transform, 0f, 0f, 1f, 0.38f, 12f, 5f, 12f, 0f);
                texts[0].text = entry.request;
                texts[1].text = entry.shouldApprove ? "Approve" : "Reject";
                texts[1].color = entry.shouldApprove
                    ? new Color(0.2f, 0.9f, 0.35f)
                    : new Color(1f, 0.3f, 0.3f);
            }

            index++;
        }
    }
    #endif
}
