using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Справочник босса для копирайтера — полностью статичные данные (DocumentRequestData.All),
// сеть не нужна. Папка открывает/закрывает список, строится один раз при первом открытии.
public class DocumentReferencePanelUI : MonoBehaviour
{
    [SerializeField] private Button folderButton;
    [SerializeField] private GameObject listPanel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject rowPrefab; // ожидается 2 TMP_Text ребёнка: [0]=заявка, [1]=вердикт

    private bool built;

    void Awake()
    {
        if (listPanel != null) listPanel.SetActive(false);
        if (folderButton != null) folderButton.onClick.AddListener(Toggle);
    }

    void Toggle()
    {
        if (listPanel == null) return;

        bool show = !listPanel.activeSelf;
        if (show && !built) Build();
        listPanel.SetActive(show);
    }

    private const float RowHeight = 30f;

    void Build()
    {
        built = true;
        if (rowContainer == null || rowPrefab == null) return;

        int index = 0;
        foreach (var entry in DocumentRequestData.All)
        {
            var row = Instantiate(rowPrefab, rowContainer);

            var rowRect = row.GetComponent<RectTransform>();
            if (rowRect != null)
                rowRect.anchoredPosition = new Vector2(0, -index * RowHeight);

            var texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = entry.request;
                texts[1].text = entry.shouldApprove ? "Согласовать" : "Отклонить";
                texts[1].color = entry.shouldApprove ? Color.green : Color.red;
            }

            index++;
        }
    }
}
