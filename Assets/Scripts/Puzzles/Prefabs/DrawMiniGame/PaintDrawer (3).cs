using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RawImage))]
public class PaintDrawer : MonoBehaviour
{
    [SerializeField] private Color brushColor = Color.black;
    [SerializeField] private int brushSize = 3;
    [SerializeField] private Color backgroundColor = Color.white;

    // === ULTIMATE SYSTEM ===
    [Header("Ultimate System")]
    public int playerId = 0; // Дизайнер
    private PlayerState playerState;
    private bool hasDrawnThisStroke = false;
    // =======================

    private RawImage rawImage;
    private Texture2D texture;
    private RectTransform rectTransform;
    private Vector2? lastTexPos;
    private bool needsApply = false;
    private bool isEraser = false;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        int w = Mathf.Max(1, (int)rectTransform.rect.width);
        int h = Mathf.Max(1, (int)rectTransform.rect.height);

        texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        ClearTexture();
        rawImage.texture = texture;

        // === ULTIMATE SYSTEM ===
        playerState = PlayerManager.Instance?.GetState(playerId);
        // =======================
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.isPressed)
        {
            Vector2 screenPos = mouse.position.ReadValue();

            // === ULTIMATE SYSTEM: инверсия мыши по Y ===
            if (playerState != null && playerState.IsMouseInverted)
                screenPos.y = Screen.height - screenPos.y;
            // ============================================

            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, null, out localPos))
            {
                Vector2 texPos = LocalToTexture(localPos);

                if (lastTexPos.HasValue)
                {
                    DrawLine(lastTexPos.Value, texPos);
                }
                else
                {
                    DrawBrush((int)texPos.x, (int)texPos.y);
                }

                lastTexPos = texPos;
            }
        }
        else
        {
            // === ULTIMATE SYSTEM: репортим при завершении штриха ===
            if (hasDrawnThisStroke)
            {
                GameEvents.ReportCorrectAction(playerId);
                hasDrawnThisStroke = false;
            }
            // =======================================================
            lastTexPos = null;
        }

        if (needsApply)
        {
            texture.Apply();
            needsApply = false;
        }
    }

    public void SetBrushMode() => isEraser = false;
    public void SetEraserMode() => isEraser = true;

    public void SetColorBlack()  { brushColor = Color.black; isEraser = false; }
    public void SetColorRed()    { brushColor = Color.red; isEraser = false; }
    public void SetColorBlue()   { brushColor = Color.blue; isEraser = false; }
    public void SetColorGreen()  { brushColor = Color.green; isEraser = false; }
    public void SetColorYellow() { brushColor = new Color(1f, 0.92f, 0.016f, 1f); isEraser = false; }

    void ClearTexture()
    {
        Color32[] pixels = new Color32[texture.width * texture.height];
        Color32 c = backgroundColor;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        texture.SetPixels32(pixels);
        texture.Apply();
    }

    Vector2 LocalToTexture(Vector2 local)
    {
        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;
        return new Vector2(local.x + w * 0.5f, local.y + h * 0.5f);
    }

    void DrawLine(Vector2 from, Vector2 to)
    {
        int x0 = (int)from.x;
        int y0 = (int)from.y;
        int x1 = (int)to.x;
        int y1 = (int)to.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawBrush(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    void DrawBrush(int cx, int cy)
    {
        Color drawColor = isEraser ? backgroundColor : brushColor;
        int r = brushSize;
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r + r)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, drawColor);
                    }
                }
            }
        }
        // === ULTIMATE SYSTEM ===
        hasDrawnThisStroke = true;
        // =======================
        needsApply = true;
    }
}