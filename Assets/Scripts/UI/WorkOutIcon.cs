using UnityEngine;
using UnityEngine.UI;

/// <summary>Small vector pictograms that stay crisp at every Canvas scale.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class WorkOutIcon : MaskableGraphic
{
    public enum Symbol { Person, People, Clock, Bars, Document, Pen, Eraser, Check, Close, Monitor, Coffee, Palette, Bell }
    public Symbol symbol;
    private VertexHelper mesh;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        mesh = vh;
        switch (symbol)
        {
            case Symbol.Person:
            case Symbol.People:
                Disc(0.5f, 0.68f, 0.18f);
                Disc(0.5f, 0.24f, 0.3f);
                if (symbol == Symbol.People) { Disc(0.83f, 0.66f, 0.13f); Line(0.84f, 0.2f, 0.84f, 0.46f, 0.2f); }
                break;
            case Symbol.Clock:
                Ring(0.5f, 0.5f, 0.41f); Line(0.5f, 0.5f, 0.5f, 0.78f); Line(0.5f, 0.5f, 0.7f, 0.38f); break;
            case Symbol.Bars:
                for (int i = 0; i < 4; i++) Line(0.16f + i * 0.22f, 0.15f, 0.16f + i * 0.22f, 0.32f + i * 0.18f, 0.12f);
                break;
            case Symbol.Document:
                Box(0.2f, 0.1f, 0.8f, 0.86f); Line(0.36f, 0.9f, 0.64f, 0.9f, 0.12f);
                for (int i = 0; i < 3; i++) Line(0.34f, 0.32f + i * 0.17f, 0.66f, 0.32f + i * 0.17f, 0.055f);
                break;
            case Symbol.Pen:
                Line(0.24f, 0.24f, 0.77f, 0.77f, 0.19f); Line(0.2f, 0.2f, 0.15f, 0.12f, 0.1f); Line(0.75f, 0.9f, 0.91f, 0.74f); break;
            case Symbol.Eraser:
                Line(0.28f, 0.32f, 0.69f, 0.73f, 0.35f); Line(0.25f, 0.15f, 0.75f, 0.15f); break;
            case Symbol.Check:
                Line(0.18f, 0.51f, 0.4f, 0.28f, 0.12f); Line(0.4f, 0.28f, 0.84f, 0.75f, 0.12f); break;
            case Symbol.Close:
                Line(0.25f, 0.25f, 0.75f, 0.75f); Line(0.25f, 0.75f, 0.75f, 0.25f); break;
            case Symbol.Monitor:
                Box(0.1f, 0.3f, 0.9f, 0.87f); Line(0.5f, 0.3f, 0.5f, 0.1f); Line(0.3f, 0.1f, 0.7f, 0.1f); break;
            case Symbol.Coffee:
                Box(0.16f, 0.18f, 0.7f, 0.7f); Box(0.7f, 0.35f, 0.88f, 0.62f); Line(0.27f, 0.83f, 0.27f, 0.95f); Line(0.51f, 0.83f, 0.51f, 0.95f); break;
            case Symbol.Palette:
                Ring(0.5f, 0.5f, 0.4f); Disc(0.32f, 0.62f, 0.07f); Disc(0.55f, 0.73f, 0.07f); Disc(0.73f, 0.51f, 0.07f); Disc(0.35f, 0.35f, 0.1f); break;
            default:
                Ring(0.5f, 0.55f, 0.3f); Line(0.17f, 0.26f, 0.83f, 0.26f); Disc(0.5f, 0.13f, 0.07f); break;
        }
    }

    private Vector2 Point(float x, float y)
    {
        Rect r = rectTransform.rect;
        float size = Mathf.Min(r.width, r.height);
        return r.center + new Vector2(x - 0.5f, y - 0.5f) * size;
    }

    private void Line(float x0, float y0, float x1, float y1, float width = 0.08f)
    {
        Vector2 a = Point(x0, y0), b = Point(x1, y1);
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * width * Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        int i = mesh.currentVertCount;
        mesh.AddVert(a - n, color, Vector2.zero); mesh.AddVert(a + n, color, Vector2.zero);
        mesh.AddVert(b + n, color, Vector2.zero); mesh.AddVert(b - n, color, Vector2.zero);
        mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
    }
    private void Box(float x0, float y0, float x1, float y1)
    { Line(x0, y0, x1, y0); Line(x1, y0, x1, y1); Line(x1, y1, x0, y1); Line(x0, y1, x0, y0); }
    private void Ring(float x, float y, float r)
    {
        for (int i = 0; i < 48; i++)
        { float a = i * Mathf.PI / 24, b = (i + 1) * Mathf.PI / 24; Line(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r, x + Mathf.Cos(b) * r, y + Mathf.Sin(b) * r, 0.075f); }
    }
    private void Disc(float x, float y, float r)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(Point(x, y), color, Vector2.zero);
        for (int i = 0; i <= 40; i++)
        { float a = i * Mathf.PI / 20; mesh.AddVert(Point(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r), color, Vector2.zero); if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1); }
    }
}
