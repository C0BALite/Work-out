using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Головоломка дизайнера: залить области картинки цветами из палитры так,
// как они раскрашены в эталоне на экране босса.
[RequireComponent(typeof(RawImage))]
public class ColoringMiniGame : MonoBehaviour, IPuzzle, IPointerClickHandler
{
    [SerializeField] private ColoringBossSync sync;
    [SerializeField] private RawImage canvasImage;

    [Header("Palette")]
    [SerializeField] private Image[] swatches = new Image[10];
    [SerializeField] private GameObject[] selectionMarks = new GameObject[10];

    [Header("Puzzle Completion")]
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text submitLabel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultValueText;

    [SerializeField] private string loadingHint = "Loading the picture...";
    [SerializeField] private string playHint = "Ask your manager for the reference colors, then click an area to fill it.";
    [SerializeField] private string submittedHint = "Submitted. Your manager can see the result.";

    public bool IsCompleted { get; private set; }

    private ColoringPicture picture;
    private Texture2D texture;
    private byte[] fills;
    private byte[] targets;
    private int selectedColor;
    private float accuracy;

    public void Begin()
    {
        IsCompleted = false;
        accuracy = 0f;
        picture = null;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (submitButton != null) submitButton.interactable = false;
        if (submitLabel != null) submitLabel.text = "Submit";
        if (hintText != null) hintText.text = loadingHint;
        SelectColor(0);
        TryInitialize();
    }

    public void ForceEnd()
    {
        if (IsCompleted) return;
        Finish();
    }

    public float GetLocalScore() => accuracy;

    public void SelectColor(int index)
    {
        selectedColor = index;
        for (int i = 0; i < selectionMarks.Length; i++)
            if (selectionMarks[i] != null) selectionMarks[i].SetActive(i == index);
    }

    public void Submit()
    {
        if (IsCompleted || picture == null) return;
        Finish();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsCompleted || picture == null) return;

        RectTransform rect = canvasImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Rect bounds = rect.rect;
        Vector2 normalized = new Vector2((local.x - bounds.xMin) / bounds.width, (local.y - bounds.yMin) / bounds.height);
        Rect uv = canvasImage.uvRect;
        int region = picture.RegionAt(uv.min + Vector2.Scale(normalized, uv.size));
        if (region < 0 || fills[region] == selectedColor) return;

        fills[region] = (byte)selectedColor;
        picture.Render(texture, fills, sync.Palette);
        sync.FillRegionRpc(region, (byte)selectedColor);
    }

    private void Update()
    {
        if (picture == null)
        {
            TryInitialize();
            return;
        }

        canvasImage.uvRect = ColoringPicture.FitUv(canvasImage.rectTransform.rect.size, picture.Width, picture.Height);
    }

    private void TryInitialize()
    {
        if (sync == null || !sync.TryGetPicture(out ColoringPicture ready)) return;

        picture = ready;
        targets = new byte[picture.RegionCount];
        fills = new byte[picture.RegionCount];
        sync.CopyTargets(targets);
        sync.CopyFills(fills);

        if (texture != null) Destroy(texture);
        texture = picture.CreateTexture();
        picture.Render(texture, fills, sync.Palette);
        canvasImage.texture = texture;
        canvasImage.uvRect = ColoringPicture.FitUv(canvasImage.rectTransform.rect.size, picture.Width, picture.Height);

        Color32[] palette = sync.Palette;
        for (int i = 0; i < swatches.Length; i++)
        {
            if (swatches[i] == null) continue;
            swatches[i].gameObject.SetActive(i < palette.Length);
            if (i < palette.Length) swatches[i].color = palette[i];
        }

        if (submitButton != null) submitButton.interactable = true;
        if (hintText != null) hintText.text = playHint;
    }

    private void Finish()
    {
        IsCompleted = true;
        accuracy = picture != null ? ColoringPicture.Accuracy(targets, fills, sync.Palette) : 0f;

        if (submitButton != null) submitButton.interactable = false;
        if (submitLabel != null) submitLabel.text = "Submitted";
        if (hintText != null) hintText.text = submittedHint;
        if (resultValueText != null) resultValueText.text = $"{accuracy * 100f:F0}%";
        if (resultPanel != null) resultPanel.SetActive(true);

        if (sync != null && sync.IsSpawned) sync.SubmitRpc();
    }

    private void OnDestroy()
    {
        if (texture != null) Destroy(texture);
    }
}
