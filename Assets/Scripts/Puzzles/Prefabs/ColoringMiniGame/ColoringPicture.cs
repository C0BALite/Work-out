using System.Collections.Generic;
using UnityEngine;

// Раскраска, разбитая на области заливки. Разбиение детерминированное,
// поэтому индексы областей совпадают у сервера, художника и босса.
public class ColoringPicture
{
    public const byte Empty = byte.MaxValue;

    private const int LineThreshold = 150;
    // Цвета дальше этого расстояния (в долях диагонали RGB-куба) считаются совсем непохожими.
    private const float SimilarityRadius = 0.35f;

    public int Width { get; }
    public int Height { get; }
    public int RegionCount { get; }

    private readonly int[] regionMap;
    private readonly byte[] luminance;
    private readonly Color32[] buffer;

    public ColoringPicture(Texture2D source)
    {
        Width = source.width;
        Height = source.height;
        Color32[] pixels = source.GetPixels32();

        int total = Width * Height;
        luminance = new byte[total];
        regionMap = new int[total];
        buffer = new Color32[total];
        for (int i = 0; i < total; i++)
        {
            Color32 p = pixels[i];
            luminance[i] = (byte)((p.r * 299 + p.g * 587 + p.b * 114) / 1000);
            regionMap[i] = -1;
        }

        RegionCount = Segment();
    }

    public int RegionAt(Vector2 uv)
    {
        if (uv.x < 0f || uv.y < 0f || uv.x >= 1f || uv.y >= 1f) return -1;
        int x = Mathf.Clamp((int)(uv.x * Width), 0, Width - 1);
        int y = Mathf.Clamp((int)(uv.y * Height), 0, Height - 1);
        return regionMap[y * Width + x];
    }

    public Texture2D CreateTexture()
    {
        return new Texture2D(Width, Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    // Штрихи и незакрашенные области остаются исходными, закрашенные умножаются на яркость
    // исходника — так сглаженные края линий не превращаются в "лесенку".
    public void Render(Texture2D target, IReadOnlyList<byte> colors, IReadOnlyList<Color32> palette)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            byte lum = luminance[i];
            int region = regionMap[i];
            byte color = region >= 0 && region < colors.Count ? colors[region] : Empty;
            if (color == Empty || color >= palette.Count)
            {
                buffer[i] = new Color32(lum, lum, lum, 255);
                continue;
            }

            Color32 fill = palette[color];
            buffer[i] = new Color32((byte)(fill.r * lum / 255), (byte)(fill.g * lum / 255), (byte)(fill.b * lum / 255), 255);
        }

        target.SetPixels32(buffer);
        target.Apply(false);
    }

    // Колода из всей палитры, перемешиваемая заново по исчерпании: цвета в эталоне
    // случайные, но не повторяются, пока не использованы все.
    public byte[] GenerateTargetColors(int paletteSize)
    {
        var result = new byte[RegionCount];
        var deck = new List<byte>(paletteSize);
        for (int i = 0; i < result.Length; i++)
        {
            if (deck.Count == 0)
            {
                for (int c = 0; c < paletteSize; c++) deck.Add((byte)c);
                for (int c = deck.Count - 1; c > 0; c--)
                {
                    int j = Random.Range(0, c + 1);
                    (deck[c], deck[j]) = (deck[j], deck[c]);
                }
            }

            result[i] = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
        }
        return result;
    }

    // 0..1: среднее по областям сходство цвета игрока с эталоном; незакрашенная область считается белой.
    public static float Accuracy(IReadOnlyList<byte> target, IReadOnlyList<byte> fills, IReadOnlyList<Color32> palette)
    {
        if (target == null || target.Count == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < target.Count; i++)
        {
            byte fill = i < fills.Count ? fills[i] : Empty;
            Color32 expected = palette[target[i]];
            Color32 actual = fill == Empty || fill >= palette.Count ? new Color32(255, 255, 255, 255) : palette[fill];
            sum += Similarity(expected, actual);
        }
        return sum / target.Count;
    }

    private static float Similarity(Color32 a, Color32 b)
    {
        float dr = (a.r - b.r) / 255f;
        float dg = (a.g - b.g) / 255f;
        float db = (a.b - b.b) / 255f;
        float distance = Mathf.Sqrt(dr * dr + dg * dg + db * db) / Mathf.Sqrt(3f);
        return 1f - Mathf.Clamp01(distance / SimilarityRadius);
    }

    // Вписывает картинку в RawImage без растяжения: поля берут белый край текстуры (wrap = Clamp).
    public static Rect FitUv(Vector2 rectSize, int textureWidth, int textureHeight)
    {
        if (rectSize.x <= 0f || rectSize.y <= 0f || textureWidth <= 0 || textureHeight <= 0)
            return new Rect(0f, 0f, 1f, 1f);

        float rectAspect = rectSize.x / rectSize.y;
        float textureAspect = (float)textureWidth / textureHeight;
        if (rectAspect > textureAspect)
        {
            float width = rectAspect / textureAspect;
            return new Rect(0.5f - width * 0.5f, 0f, width, 1f);
        }

        float height = textureAspect / rectAspect;
        return new Rect(0f, 0.5f - height * 0.5f, 1f, height);
    }

    // Области — 4-связные компоненты светлых пикселей. Фон (касается края) и мелкие
    // обрывки между штрихами не закрашиваются и в счёт не идут.
    private int Segment()
    {
        int total = Width * Height;
        int minPixels = Mathf.Max(60, total / 1000);
        var visited = new bool[total];
        var component = new List<int>(total / 4);
        var stack = new Stack<int>();
        int regions = 0;

        for (int start = 0; start < total; start++)
        {
            if (visited[start] || luminance[start] < LineThreshold) continue;

            component.Clear();
            bool touchesBorder = false;
            visited[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int index = stack.Pop();
                component.Add(index);
                int x = index % Width;
                int y = index / Width;
                if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1) touchesBorder = true;

                if (x > 0) Visit(index - 1, visited, stack);
                if (x < Width - 1) Visit(index + 1, visited, stack);
                if (y > 0) Visit(index - Width, visited, stack);
                if (y < Height - 1) Visit(index + Width, visited, stack);
            }

            if (touchesBorder || component.Count < minPixels) continue;

            foreach (int index in component) regionMap[index] = regions;
            regions++;
        }
        return regions;
    }

    private void Visit(int index, bool[] visited, Stack<int> stack)
    {
        if (visited[index] || luminance[index] < LineThreshold) return;
        visited[index] = true;
        stack.Push(index);
    }
}
