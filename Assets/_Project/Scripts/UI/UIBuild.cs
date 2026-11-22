using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Спільні «цеглинки» для вікон, які будуються з коду (HUD, мапа): Canvas, прямокутник, текст, кнопка.
// Щоб у кожному скрипті не повторювати однакові 10 рядків.
public static class UIBuild
{
    // Вбудований шрифт Unity — кирилиця є
    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // Canvas на весь екран, масштаб під 1920×1080
    public static void MakeCanvas(GameObject go, int sortingOrder)
    {
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Прямокутник із кольором; anchor — куди «прив'язаний» (0,0 — лівий нижній кут, 0.5,0.5 — центр)
    public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        RectTransform r = Rect(name, parent);
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    // Текст на весь батьківський прямокутник
    public static Text Label(string name, Transform parent, int size, FontStyle style, TextAnchor align, Color color)
    {
        RectTransform r = Rect(name, parent);
        Stretch(r);
        var t = r.gameObject.AddComponent<Text>();
        t.font = DefaultFont;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // Кнопка з підписом
    public static Button MakeButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string text, UnityAction onClick)
    {
        Image bg = Panel(name, parent, anchor, position, size, new Color(0.25f, 0.21f, 0.17f, 0.92f));
        var button = bg.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        Label("Text", bg.transform, 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f)).text = text;
        return button;
    }

    // Рамка з 4 тонких смужок (для району «Зникло» — лише контур)
    public static void Frame(Transform parent, float thickness, Color color)
    {
        Bar(parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -thickness), Vector2.zero, color);   // верх
        Bar(parent, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, thickness), color);    // низ
        Bar(parent, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(thickness, 0), color);    // ліво
        Bar(parent, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-thickness, 0), Vector2.zero, color);   // право
    }

    public static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Bar(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform r = Rect("Line", parent);
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // Спрайт, намальований кодом (поки немає арту): draw(x, y) повертає колір пікселя
    public static Sprite DrawSprite(int size, Func<float, float, Color> draw)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, draw((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f));   // координати від −1 до 1
        tex.Apply();
        return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
