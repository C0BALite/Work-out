using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(RawImage))]
public class PaintDrawer : MonoBehaviour, IPuzzle   // добавлен IPuzzle
{
    [SerializeField] private Color brushColor = Color.black;
    [SerializeField] private int brushSize = 3;
    [SerializeField] private Color backgroundColor = Color.white;

    

    [Header("Puzzle Completion")]
    [SerializeField] private Button doneButton; // новое — добавить в Prefab
    public bool IsCompleted { get; private set; } // новое

    [SerializeField] private Image brushButtonImage;
    [SerializeField] private Image eraserButtonImage;

    private RawImage rawImage;
    private Texture2D texture;
    private RectTransform rectTransform;
    private Vector2? lastTexPos;
    private bool needsApply = false;
    private bool isEraser = false;

    void Start()
    {
        Canvas.ForceUpdateCanvases();
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        int w = Mathf.Max(1, (int)rectTransform.rect.width);
        int h = Mathf.Max(1, (int)rectTransform.rect.height);

        texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        ClearTexture();
        rawImage.texture = texture;

        

        if (doneButton != null)
        {
            doneButton.onClick.RemoveListener(OnDoneClicked);
            doneButton.onClick.AddListener(OnDoneClicked);
        }
    }

    public void SetSubmitButton(Button button)
    {
        if (doneButton != null) doneButton.onClick.RemoveListener(OnDoneClicked);
        doneButton = button;
        if (doneButton != null) doneButton.onClick.AddListener(OnDoneClicked);
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || texture == null || IsCompleted) return;

        if (mouse.leftButton.isPressed)
        {
            Vector2 screenPos = mouse.position.ReadValue();

            

            Vector2 localPos;
            var canvas = GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPos, eventCamera) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, eventCamera, out localPos))
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
            else lastTexPos = null;
        }
        else
        {
            
            lastTexPos = null;
        }

        if (needsApply)
        {
            texture.Apply();
            needsApply = false;
        }
    }

    public void SetBrushMode() { isEraser = false; RefreshToolSelection(); }
    public void SetEraserMode() { isEraser = true; RefreshToolSelection(); }

    private void RefreshToolSelection()
    {
        if (brushButtonImage != null) brushButtonImage.sprite = WorkOutTheme.GetSprite(isEraser ? WorkOutTheme.Surface.Raised : WorkOutTheme.Surface.Blue);
        if (eraserButtonImage != null) eraserButtonImage.sprite = WorkOutTheme.GetSprite(isEraser ? WorkOutTheme.Surface.Blue : WorkOutTheme.Surface.Raised);
    }

    public void SetColorBlack()  { brushColor = Color.black; SetBrushMode(); }
    public void SetColorRed()    { brushColor = Color.red; SetBrushMode(); }
    public void SetColorBlue()   { brushColor = Color.blue; SetBrushMode(); }
    public void SetColorGreen()  { brushColor = Color.green; SetBrushMode(); }
    public void SetColorYellow() { brushColor = new Color(1f, 0.92f, 0.016f, 1f); SetBrushMode(); }

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
        return new Vector2((local.x - rectTransform.rect.xMin) / w * (texture.width - 1),
            (local.y - rectTransform.rect.yMin) / h * (texture.height - 1));
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
        
        needsApply = true;
    }
    public void Begin() // новое
    {
        IsCompleted = false;
        lastTexPos = null;
        if (doneButton != null)
        {
            doneButton.interactable = true;
            var label = doneButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) label.text = "Submit";
        }
        if (texture != null) ClearTexture();
    }

    public void ForceEnd() // новое
    {
        IsCompleted = true;
    }

    // Когда игрок сделал правильное действие:
    private void OnCorrectAction()
    {
        // Получаем NetworkObject текущего игрока
        // (зависит от вашей реализации, как определяется "текущий игрок")
        ulong myId = NetworkManager.Singleton.LocalClientId;

        // Сообщаем системе событий
        MiniGameEventSystem.Instance.ReportCorrectAction(myId);
    }

    public float GetLocalScore() => IsCompleted ? 1f : 0f; // новое

    void OnDoneClicked() // новое
    {
        if (IsCompleted) return;
        IsCompleted = true;
        if (doneButton != null)
        {
            doneButton.interactable = false;
            var label = doneButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) label.text = "Submitted";
        }
        OnCorrectAction();
    }

    private void OnDestroy()
    {
        if (doneButton != null) doneButton.onClick.RemoveListener(OnDoneClicked);
        if (texture != null) Destroy(texture);
    }
}
