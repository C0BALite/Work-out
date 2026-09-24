using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Панель дизайнера на экране босса: эталонная раскраска, живой холст дизайнера
// и итоговый процент совпадения после отправки.
public class ColoringBossPanelUI : MonoBehaviour
{
    [SerializeField] private RawImage referenceImage;
    [SerializeField] private RawImage liveImage;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Image statusBackground;

    [SerializeField] private Color pendingColor = new Color(0.25f, 0.28f, 0.36f, 1f);
    [SerializeField] private Color goodResultColor = new Color(0.15f, 0.75f, 0.28f, 1f);
    [SerializeField] private Color poorResultColor = new Color(0.85f, 0.2f, 0.26f, 1f);
    [SerializeField, Range(0f, 1f)] private float goodResultThreshold = 0.6f;

    private ColoringBossSync boundSync;
    private ColoringPicture picture;
    private Texture2D referenceTexture;
    private Texture2D liveTexture;
    private byte[] targets;
    private byte[] fills;
    private byte[] shownFills;

    private void Awake()
    {
        ShowPictures(false);
        SetStatus("Waiting for designer", pendingColor);
    }

    private void Update()
    {
        ColoringBossSync sync = ColoringBossSync.Instance;
        if (sync == null || !sync.TryGetPicture(out ColoringPicture ready))
        {
            ShowPictures(false);
            SetStatus("Waiting for designer", pendingColor);
            return;
        }

        if (sync != boundSync || ready != picture)
            Bind(sync, ready);

        ShowPictures(true);
        FitImage(referenceImage);
        FitImage(liveImage);

        sync.CopyFills(fills);
        if (!SameFills())
        {
            fills.CopyTo(shownFills, 0);
            picture.Render(liveTexture, fills, sync.Palette);
        }

        if (sync.Submitted.Value)
        {
            float accuracy = sync.Accuracy.Value;
            SetStatus($"Color match: {accuracy * 100f:F0}%", accuracy >= goodResultThreshold ? goodResultColor : poorResultColor);
        }
        else
        {
            SetStatus($"Colored {CountFilled()} of {fills.Length} areas", pendingColor);
        }
    }

    private void Bind(ColoringBossSync sync, ColoringPicture ready)
    {
        boundSync = sync;
        picture = ready;
        targets = new byte[picture.RegionCount];
        fills = new byte[picture.RegionCount];
        shownFills = new byte[picture.RegionCount];
        sync.CopyTargets(targets);
        sync.CopyFills(fills);
        fills.CopyTo(shownFills, 0);

        ReleaseTextures();
        referenceTexture = picture.CreateTexture();
        liveTexture = picture.CreateTexture();
        picture.Render(referenceTexture, targets, sync.Palette);
        picture.Render(liveTexture, fills, sync.Palette);
        if (referenceImage != null) referenceImage.texture = referenceTexture;
        if (liveImage != null) liveImage.texture = liveTexture;
    }

    private bool SameFills()
    {
        for (int i = 0; i < fills.Length; i++)
            if (fills[i] != shownFills[i]) return false;
        return true;
    }

    private int CountFilled()
    {
        int count = 0;
        foreach (byte fill in fills)
            if (fill != ColoringPicture.Empty) count++;
        return count;
    }

    private void FitImage(RawImage image)
    {
        if (image != null)
            image.uvRect = ColoringPicture.FitUv(image.rectTransform.rect.size, picture.Width, picture.Height);
    }

    private void ShowPictures(bool visible)
    {
        if (referenceImage != null) referenceImage.enabled = visible;
        if (liveImage != null) liveImage.enabled = visible;
    }

    private void SetStatus(string text, Color background)
    {
        if (statusLabel != null) statusLabel.text = text;
        if (statusBackground != null) statusBackground.color = background;
    }

    private void ReleaseTextures()
    {
        if (referenceTexture != null) Destroy(referenceTexture);
        if (liveTexture != null) Destroy(liveTexture);
    }

    private void OnDestroy()
    {
        ReleaseTextures();
    }
}
