using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared, resolution-independent desktop skin. No screenshot textures or external assets.</summary>
public static class WorkOutTheme
{
    public enum Surface { Panel, Raised, Inset, Header, Blue, Red, Green, Paper, Track, Tint }
    public static readonly Color Ink = new Color32(237, 242, 255, 255);
    public static readonly Color Muted = new Color32(151, 169, 204, 255);
    public static readonly Color Blue = new Color32(75, 134, 255, 255);
    public static readonly Color Green = new Color32(86, 227, 91, 255);
    public static readonly Color Red = new Color32(255, 61, 80, 255);
    public static readonly Color PaperInk = new Color32(36, 48, 76, 255);
    public static readonly Color[] PlayerColors = { Blue, new Color32(160, 103, 255, 255), Green, new Color32(255, 191, 69, 255) };
    private static readonly Dictionary<Surface, Sprite> Sprites = new Dictionary<Surface, Sprite>();

    public static Sprite GetSprite(Surface surface)
    {
        if (Sprites.TryGetValue(surface, out var existing) && existing != null) return existing;
        var saved = Resources.Load<Sprite>("WorkOutUI/" + surface);
        if (saved != null) { Sprites[surface] = saved; return saved; }
#if UNITY_EDITOR
        Color top, bottom, edge;
        switch (surface)
        {
            case Surface.Raised: top = Hex(0x303f64); bottom = Hex(0x1c2643); edge = Hex(0x56658c); break;
            case Surface.Inset: top = Hex(0x121c33); bottom = Hex(0x1c2947); edge = Hex(0x344567); break;
            case Surface.Header: top = Hex(0x344e86); bottom = Hex(0x20335d); edge = Hex(0x5373ad); break;
            case Surface.Blue: top = Hex(0x4598ff); bottom = Hex(0x1553ce); edge = Hex(0x7ac6ff); break;
            case Surface.Red: top = Hex(0xff4559); bottom = Hex(0xd90d29); edge = Hex(0xff8790); break;
            case Surface.Green: top = Hex(0x72d944); bottom = Hex(0x249a26); edge = Hex(0xabf377); break;
            case Surface.Paper: top = Hex(0xffffff); bottom = Hex(0xe3ebf7); edge = Hex(0xc1cde2); break;
            case Surface.Track: top = Hex(0x526184); bottom = Hex(0x3b496b); edge = Hex(0x637195); break;
            case Surface.Tint: top = Color.white; bottom = Hex(0x9babc6); edge = Color.white; break;
            default: top = Hex(0x253555); bottom = Hex(0x111b30); edge = Hex(0x435881); break;
        }
        const int size = 128;
        const float radius = 23f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "WorkOut " + surface, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 q = new Vector2(Mathf.Abs(x - 63.5f), Mathf.Abs(y - 63.5f)) - Vector2.one * (63f - radius);
            float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
            Color c = Color.Lerp(bottom, top, y / 127f);
            float bevel = Mathf.Clamp01(1f - Mathf.Abs(distance + 2f) / 1.4f);
            c = Color.Lerp(c, edge, bevel * (0.4f + 0.6f * y / 127f));
            c.a = Mathf.Clamp01(-distance + 0.2f);
            pixels[y * size + x] = c;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(28, 28, 28, 28));
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        Sprites[surface] = sprite;
        return sprite;
#else
        Debug.LogError("Missing authored UI sprite: " + surface);
        return null;
#endif
    }

    public static Color Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

#if UNITY_EDITOR
    public static void ClearSpriteCache() => Sprites.Clear();

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Layout coordinates are normalized, bottom-left to top-right, with optional insets.
    public static void Place(Transform target, float x0, float y0, float x1, float y1, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        if (!(target is RectTransform rect)) return;
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.pivot = Vector2.one * 0.5f;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    public static void Fixed(Transform target, float x, float y, float w, float h, float ax = 0, float ay = 1)
    {
        if (!(target is RectTransform rect)) return;
        rect.anchorMin = rect.anchorMax = new Vector2(ax, ay);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        rect.localScale = Vector3.one;
    }

    public static Image Skin(Transform target, Surface surface = Surface.Panel, bool shadow = false)
    {
        var img = target.GetComponent<Image>();
        if (img == null) img = target.gameObject.AddComponent<Image>();
        img.sprite = GetSprite(surface);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1;
        img.color = Color.white;
        if (shadow)
        {
            var s = target.GetComponent<Shadow>() ?? target.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0.01f, 0.02f, 0.07f, 0.4f);
            s.effectDistance = new Vector2(0, -7);
        }
        return img;
    }

    public static RectTransform Panel(string name, Transform parent, Surface surface = Surface.Panel, bool shadow = true)
    {
        var rect = Rect(name, parent);
        Skin(rect, surface, shadow).raycastTarget = false;
        return rect;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, string value, float size = 22, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var rect = Rect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = value;
        Text(label, size, color, alignment);
        Place(rect, 0, 0, 1, 1);
        return label;
    }

    public static void Text(TMP_Text text, float size = 22, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        if (text == null) return;
        text.color = color ?? Ink;
        text.fontSize = size;
        text.fontStyle = FontStyles.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(14, size);
        text.fontSizeMax = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    public static void Button(Button button, Surface surface = Surface.Raised, string caption = null)
    {
        if (button == null) return;
        button.targetGraphic = Skin(button.transform, surface, true);
        button.transition = Selectable.Transition.ColorTint;
        var colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.7f, 0.78f, 0.95f);
        colors.selectedColor = new Color(0.85f, 0.93f, 1f);
        colors.disabledColor = new Color(0.48f, 0.52f, 0.64f, 0.65f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            Text(label, 22, Ink, TextAlignmentOptions.Center);
            if (caption != null) label.text = caption;
            Place(label.transform, 0, 0, 1, 1, 12, 5, 12, 5);
        }
    }

    public static Button NewButton(string name, Transform parent, string caption, Surface surface = Surface.Raised)
    {
        var rect = Rect(name, parent);
        var button = rect.gameObject.AddComponent<Button>();
        Label("Caption", rect, caption);
        Button(button, surface, caption);
        return button;
    }

    public static RectTransform Window(Transform root, string title, WorkOutIcon.Symbol symbol)
    {
        var bg = Panel("DesktopWindow", root);
        Place(bg, 0, 0, 1, 1);
        bg.SetAsFirstSibling();
        var header = Panel("WindowHeader", bg, Surface.Header, false);
        Place(header, 0, 1, 1, 1, 0, -64, 0, 0);
        Fixed(Icon(header, symbol).transform, 22, -17, 30, 30);
        Place(Label("WindowTitle", header, title, 26).transform, 0, 0, 1, 1, 68, 0, 28, 0);
        return bg;
    }

    public static WorkOutIcon Icon(Transform parent, WorkOutIcon.Symbol symbol, Color? color = null)
    {
        var rect = Rect(symbol + "Icon", parent);
        var icon = rect.gameObject.AddComponent<WorkOutIcon>();
        icon.symbol = symbol;
        icon.color = color ?? Ink;
        icon.raycastTarget = false;
        return icon;
    }

    public static void Slider(Slider slider, bool budget = false)
    {
        if (slider == null) return;
        var background = slider.transform.Find("Background");
        if (background != null)
        {
            Skin(background, Surface.Track).raycastTarget = true;
            Place(background, 0, 0.35f, 1, 0.65f);
            background.SetAsFirstSibling();
        }
        if (slider.fillRect != null)
        {
            Skin(slider.fillRect, budget ? Surface.Green : Surface.Blue).raycastTarget = false;
            Place(slider.fillRect.parent, 0, 0.35f, 1, 0.65f, 3, 0, 3, 0);
        }
        if (slider.handleRect != null)
        {
            var handle = slider.handleRect;
            Skin(handle, Surface.Red, true);
            handle.sizeDelta = new Vector2(24, 0);
            slider.targetGraphic = handle.GetComponent<Image>();
            handle.parent.SetAsLastSibling();
            var colors = ColorBlock.defaultColorBlock;
            colors.pressedColor = new Color(1, 0.8f, 0.8f);
            slider.colors = colors;
        }
    }

    public static void ScaleCanvas(GameObject go)
    {
        var scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
    }
#endif
}
