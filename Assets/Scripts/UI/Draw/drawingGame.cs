using System.Collections.Generic; // ← ДОБАВИЛ для List<>
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DrawingGame : MonoBehaviour
{
    private Color currentColor = Color.black;
    private float brushSize = 5f;
    private bool isEraser = false;
    
    private Canvas canvas;
    private RectTransform drawingArea;
    private RawImage drawingSurface;
    private Texture2D drawTexture;
    private int textureWidth = 1024;
    private int textureHeight = 1024;
    
    private Vector2Int? lastPixelPos;
    private Mouse mouse;
    private bool isDrawing = false;
    
    private readonly Color[] presetColors = new Color[]
    {
        Color.black, Color.red, Color.green, Color.blue, Color.yellow, 
        Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f)
    };

    private void Awake()
    {
        mouse = Mouse.current;
        if (mouse == null)
        {
            Debug.LogError("[DrawingGame] Mouse not found!");
            return;
        }
        
        CreateCanvas();
        CreateDrawingArea();
        CreateToolbar();
        CreateColorPanel();
        CreateSizePanel();
    }
    
    private void CreateCanvas()
    {
        var canvasObj = new GameObject("Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        var existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (existingEventSystem != null) Destroy(existingEventSystem.gameObject);
        
        var eventObj = new GameObject("EventSystem");
        eventObj.AddComponent<EventSystem>();
        var inputModuleType = typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule);
        if (inputModuleType != null) eventObj.AddComponent(inputModuleType);
    }
    
    private void CreateDrawingArea()
    {
        var areaObj = new GameObject("DrawingArea", typeof(RectTransform));
        areaObj.transform.SetParent(canvas.transform, false);
        
        drawingArea = areaObj.GetComponent<RectTransform>();
        drawingArea.anchorMin = new Vector2(0.15f, 0.15f);
        drawingArea.anchorMax = new Vector2(0.85f, 0.85f);
        drawingArea.offsetMin = Vector2.zero;
        drawingArea.offsetMax = Vector2.zero;
        
        drawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        drawTexture.filterMode = FilterMode.Point;
        
        for (int x = 0; x < textureWidth; x++)
        {
            for (int y = 0; y < textureHeight; y++)
            {
                drawTexture.SetPixel(x, y, Color.white);
            }
        }
        drawTexture.Apply();
        
        drawingSurface = areaObj.AddComponent<RawImage>();
        drawingSurface.texture = drawTexture;
        drawingSurface.raycastTarget = false;
    }
    
    private void CreateToolbar()
    {
        var panelObj = CreatePanel("Toolbar", canvas.transform,
            new Vector2(0, 0), new Vector2(0.14f, 1),
            new Color(0.15f, 0.15f, 0.15f, 0.95f));
        
        var vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 60, 10);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        
        CreateText("ToolsTitle", panelObj.transform, "TOOLS", 22, TextAnchor.MiddleCenter);
        
        // Brush
        var brushBtn = CreateButton("Brush", panelObj.transform, new Vector2(130, 45),
            new Color(0.9f, 0.7f, 0.1f), Color.white, 16);
        brushBtn.onClick.AddListener(() => SelectTool(false, brushBtn));
        
        // Eraser
        var eraserBtn = CreateButton("Eraser", panelObj.transform, new Vector2(130, 45),
            new Color(0.3f, 0.3f, 0.3f), Color.white, 16);
        eraserBtn.onClick.AddListener(() => SelectTool(true, eraserBtn));
        
        // Clear
        var clearBtn = CreateButton("Clear", panelObj.transform, new Vector2(130, 45),
            new Color(0.3f, 0.3f, 0.3f), Color.white, 16);
        clearBtn.onClick.AddListener(ClearCanvas);
    }
    
    private void CreateColorPanel()
    {
        var panelObj = CreatePanel("ColorPanel", canvas.transform,
            new Vector2(0.86f, 0.5f), new Vector2(1, 0.85f),
            new Color(0.15f, 0.15f, 0.15f, 0.95f));
        
        var glg = panelObj.AddComponent<GridLayoutGroup>();
        glg.padding = new RectOffset(10, 10, 50, 10);
        glg.cellSize = new Vector2(70, 45);
        glg.spacing = new Vector2(10, 10);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        
        CreateText("ColorsTitle", panelObj.transform, "COLORS", 20, TextAnchor.MiddleCenter);
        
        for (int i = 0; i < presetColors.Length; i++)
        {
            Color color = presetColors[i];
            var btn = CreateButton($"Color_{i}", panelObj.transform, new Vector2(70, 45),
                color, Color.white, 0);
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null) { txt.text = ""; txt.raycastTarget = false; }
            btn.onClick.AddListener(() => SetColor(color));
        }
        
        var previewObj = CreatePanel("ColorPreview", panelObj.transform,
            new Vector2(0.1f, 0), new Vector2(0.9f, 0.15f), currentColor);
        var outline = previewObj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, 2);
    }
    
    private void CreateSizePanel()
    {
        var panelObj = CreatePanel("SizePanel", canvas.transform,
            new Vector2(0.15f, 0), new Vector2(0.85f, 0.14f),
            new Color(0.15f, 0.15f, 0.15f, 0.95f));
        
        var hlg = panelObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(20, 20, 20, 20);
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        
        var labelObj = CreateText("SizeLabel", panelObj.transform, "BRUSH SIZE", 18, TextAnchor.MiddleLeft);
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 40);
        
        var sliderObj = new GameObject("SizeSlider", typeof(RectTransform));
        sliderObj.transform.SetParent(panelObj.transform, false);
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
        
        var slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 1; slider.maxValue = 50; slider.value = brushSize;
        
        var bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(0, -6); bgRect.offsetMax = new Vector2(0, 6);
        bgObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        
        var fillAreaObj = new GameObject("FillArea", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 0); fillAreaRect.offsetMax = new Vector2(-5, 0);
        
        var fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        fillObj.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
        
        var handleSlideObj = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideObj.transform.SetParent(sliderObj.transform, false);
        var handleSlideRect = handleSlideObj.GetComponent<RectTransform>();
        handleSlideRect.anchorMin = Vector2.zero; handleSlideRect.anchorMax = Vector2.one;
        handleSlideRect.pivot = new Vector2(0.5f, 0.5f);
        handleSlideRect.offsetMin = new Vector2(10, 0); handleSlideRect.offsetMax = new Vector2(-10, 0);
        
        var handleObj = new GameObject("Handle", typeof(RectTransform));
        handleObj.transform.SetParent(handleSlideObj.transform, false);
        handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 35);
        handleObj.AddComponent<Image>().color = Color.white;
        
        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.handleRect = handleObj.GetComponent<RectTransform>();
        slider.targetGraphic = handleObj.GetComponent<Image>();
        slider.onValueChanged.AddListener(OnSizeChanged);
        
        var valueTextObj = CreateText("SizeValue", panelObj.transform, "5px", 20, TextAnchor.MiddleRight);
        valueTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);
        
        var previewObj = new GameObject("SizePreview", typeof(RectTransform));
        previewObj.transform.SetParent(panelObj.transform, false);
        previewObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
        previewObj.AddComponent<Image>().color = currentColor;
    }
    
    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var img = obj.AddComponent<Image>();
        img.color = color; img.raycastTarget = true;
        return obj;
    }
    
    private Button CreateButton(string text, Transform parent, Vector2 size, Color bgColor, Color textColor, int fontSize)
    {
        var btnObj = new GameObject(text + "Button", typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        btnObj.GetComponent<RectTransform>().sizeDelta = size;
        btnObj.AddComponent<Image>().color = bgColor;
        var btn = btnObj.AddComponent<Button>();
        btn.colors = new ColorBlock
        {
            normalColor = bgColor,
            highlightedColor = new Color(bgColor.r + 0.2f, bgColor.g + 0.2f, bgColor.b + 0.2f),
            pressedColor = new Color(bgColor.r - 0.2f, bgColor.g - 0.2f, bgColor.b - 0.2f),
            selectedColor = bgColor,
            disabledColor = new Color(bgColor.r, bgColor.g, bgColor.b, 0.5f),
            colorMultiplier = 1f, fadeDuration = 0.1f
        };
        var txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
        var txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = text; txt.fontSize = fontSize; txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter; txt.raycastTarget = false;
        return btn;
    }
    
    private GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
        var txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = text; txt.fontSize = fontSize; txt.color = Color.white;
        txt.alignment = alignment; txt.fontStyle = FontStyle.Bold; txt.raycastTarget = false;
        return obj;
    }
    
    private void Update()
    {
        if (mouse == null) return;
        
        Vector2 mousePos = mouse.position.ReadValue();
        bool leftPressed = mouse.leftButton.isPressed;
        
        if (leftPressed && !isDrawing)
        {
            if (IsPointerOverUIElement(mousePos)) return;
        }
        
        Vector2Int? pixelPos = ScreenToTexturePixel(mousePos);
        
        if (!pixelPos.HasValue)
        {
            if (isDrawing) EndDrawing();
            return;
        }
        
        Vector2Int pos = pixelPos.Value;
        
        if (leftPressed && !isDrawing)
        {
            StartDrawing(pos);
        }
        else if (leftPressed && isDrawing)
        {
            ContinueDrawing(pos);
        }
        else if (!leftPressed && isDrawing)
        {
            EndDrawing();
        }
    }
    
    private Vector2Int? ScreenToTexturePixel(Vector2 screenPos)
    {
        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea, screenPos, null, out localPos))
        {
            return null;
        }
        
        Rect rect = drawingArea.rect;
        if (!rect.Contains(localPos))
            return null;
        
        float u = (localPos.x - rect.x) / rect.width;
        float v = (localPos.y - rect.y) / rect.height;
        
        int x = Mathf.RoundToInt(u * (textureWidth - 1));
        int y = Mathf.RoundToInt(v * (textureHeight - 1));
        
        return new Vector2Int(x, y);
    }
    
    private bool IsPointerOverUIElement(Vector2 screenPos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            if (result.gameObject == drawingArea.gameObject || 
                result.gameObject == drawingSurface.gameObject) continue;
            return true;
        }
        return false;
    }
    
    private void StartDrawing(Vector2Int pos)
    {
        isDrawing = true;
        lastPixelPos = pos;
        DrawBrushPixel(pos.x, pos.y);
        drawTexture.Apply();
    }
    
    private void ContinueDrawing(Vector2Int pos)
    {
        if (!lastPixelPos.HasValue) return;
        
        if (lastPixelPos.Value != pos)
        {
            DrawLine(lastPixelPos.Value.x, lastPixelPos.Value.y, pos.x, pos.y);
            drawTexture.Apply();
            lastPixelPos = pos;
        }
    }
    
    private void EndDrawing()
    {
        isDrawing = false;
        lastPixelPos = null;
    }
    
    private void ClearCanvas()
    {
        for (int x = 0; x < textureWidth; x++)
        {
            for (int y = 0; y < textureHeight; y++)
            {
                drawTexture.SetPixel(x, y, Color.white);
            }
        }
        drawTexture.Apply();
    }
    
    private void DrawBrushPixel(int x, int y)
    {
        Color drawColor = isEraser ? Color.white : currentColor;
        int radius = Mathf.RoundToInt(brushSize / 2);
        
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i*i + j*j <= radius*radius)
                {
                    int px = x + i;
                    int py = y + j;
                    if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                    {
                        drawTexture.SetPixel(px, py, drawColor);
                    }
                }
            }
        }
    }
    
    private void DrawLine(int x0, int y0, int x1, int y1)
    {
        Color drawColor = isEraser ? Color.white : currentColor;
        int radius = Mathf.RoundToInt(brushSize / 2);
        
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (i*i + j*j <= radius*radius)
                    {
                        int px = x0 + i;
                        int py = y0 + j;
                        if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                        {
                            drawTexture.SetPixel(px, py, drawColor);
                        }
                    }
                }
            }
            
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
    
    private void SelectTool(bool eraser, Button button)
    {
        isEraser = eraser;
        
        var toolbar = GameObject.Find("Toolbar");
        if (toolbar != null)
        {
            foreach (Transform child in toolbar.transform)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    btn.image.color = new Color(0.3f, 0.3f, 0.3f);
                }
            }
        }
        
        button.image.color = new Color(0.9f, 0.7f, 0.1f);
    }
    
    private void SetColor(Color color)
    {
        currentColor = color;
        var panel = GameObject.Find("ColorPanel");
        if (panel != null)
        {
            foreach (Transform child in panel.transform)
            {
                if (child.name == "ColorPreview")
                {
                    child.GetComponent<Image>().color = currentColor;
                    break;
                }
            }
        }
        UpdateSizePreview();
    }
    
    private void OnSizeChanged(float value)
    {
        brushSize = value;
        var txt = GameObject.Find("SizeValue")?.GetComponent<Text>();
        if (txt != null) txt.text = $"{value:F0}px";
        UpdateSizePreview();
    }
    
    private void UpdateSizePreview()
    {
        var preview = GameObject.Find("SizePreview");
        if (preview != null)
        {
            var img = preview.GetComponent<Image>();
            img.color = isEraser ? Color.gray : currentColor;
            float size = Mathf.Lerp(10f, 80f, (brushSize - 1f) / 49f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }
}
