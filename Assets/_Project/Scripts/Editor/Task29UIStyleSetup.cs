#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Задача 29: меню LastLetter → Task 29: UI style and Cyrillic font.
// 1) Імпортує TMP Essential Resources (якщо їх ще немає) — після першого імпорту запусти меню ще раз.
// 2) Створює TMP-шрифт Lora SDF (з кирилицею) і ставить його шрифтом за замовчуванням для TMP.
// 3) У префабах UI_Bag, UI_Dialogue, UI_PauseMenu, UI_ThreadPuzzle і в сцені MainMenu
//    замінює Legacy Text на TextMeshPro і ставить спрайти: панелі, кнопки, клітинки сумки.
// 4) У головне меню кладе фон Площі Пошти.
public static class Task29UIStyleSetup
{
    private const string UiArt = "Assets/_Project/Art/UI/";
    private const string FontsDir = "Assets/_Project/Art/UI/Fonts/";
    private const string MenuScene = "Assets/_Project/Scenes/MainMenu.unity";
    private const string MenuBg = "Assets/_Project/Art/Backgrounds/bg_intro_postoffice.png";

    private static readonly string[] Prefabs =
    {
        "Assets/_Project/Prefabs/UI/UI_Bag.prefab",
        "Assets/_Project/Prefabs/UI/UI_Dialogue.prefab",
        "Assets/_Project/Prefabs/UI/UI_PauseMenu.prefab",
        "Assets/_Project/Prefabs/UI/UI_ThreadPuzzle.prefab",
    };

    // Ці символи одразу кладемо в атлас шрифту (решту TMP додасть сам — шрифт динамічний)
    private const string Chars =
        "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюя" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        " .,:;!?()[]-–—+=/%\"'’«»…№*▼←→";

    private static readonly Color Gold = new Color(0.69f, 0.56f, 0.36f, 1f);

    private static Sprite panel, button, slot;

    [MenuItem("LastLetter/Task 29: UI style and Cyrillic font")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. TMP Essentials: шейдери й налаштування TextMeshPro
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.LogWarning("Задача 29: імпортую TMP Essential Resources. Коли Unity закінчить імпорт — запусти LastLetter → Task 29 ще раз.");
            return;
        }

        // 2. Спрайти UI (9-slice: краї не розтягуються)
        panel = ImportSprite(UiArt + "ui_panel.png", 32);
        button = ImportSprite(UiArt + "ui_button.png", 20);
        slot = ImportSprite(UiArt + "ui_slot.png", 22);
        ImportSprite(UiArt + "ui_map_frame.png", 80);
        ImportSprite(UiArt + "icon_bag.png", 0);
        ImportSprite(UiArt + "icon_map.png", 0);

        // 3. Шрифт
        TMP_FontAsset regular = MakeFont("Lora-Regular");
        TMP_FontAsset bold = MakeFont("Lora-Bold");
        TMP_FontAsset italic = MakeFont("Lora-Italic");
        if (regular == null) return;

        // жирний і курсив — справжні накреслення Lora, а не «намальовані»
        var fso = new SerializedObject(regular);
        SerializedProperty table = fso.FindProperty("m_FontWeightTable");
        if (table != null && table.arraySize > 7)
        {
            table.GetArrayElementAtIndex(4).FindPropertyRelative("italicTypeface").objectReferenceValue = italic;  // 400 курсив
            table.GetArrayElementAtIndex(7).FindPropertyRelative("regularTypeface").objectReferenceValue = bold;   // 700 жирний
            fso.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(regular);

        var settings = new SerializedObject(Resources.Load<TMP_Settings>("TMP Settings"));
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
        settings.ApplyModifiedPropertiesWithoutUndo();

        // 4. Префаби
        foreach (string path in Prefabs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            StyleTree(root.transform, regular);
            FixReferences(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 5. Головне меню
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) != null)
        {
            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.GetComponent<Canvas>() != null && !PrefabUtility.IsPartOfPrefabInstance(go))
                    StyleTree(go.transform, regular);
            AddMenuBackdrop();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Задача 29: готово — шрифт Lora (кирилиця) у TMP, нові панелі й кнопки. Відкрий MainMenu і натисни Play.");
    }

    // ---------- Шрифт ----------

    private static TMP_FontAsset MakeFont(string fileName)
    {
        string assetPath = FontsDir + fileName + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
        {
            existing.TryAddCharacters(Chars, out _);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontsDir + fileName + ".ttf");
        if (font == null) { Debug.LogError("Не знайдено шрифт " + FontsDir + fileName + ".ttf"); return null; }

        // динамічний SDF-шрифт: літер, яких немає в атласі, TMP додасть сам — «квадратиків» не буде
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
                                                         AtlasPopulationMode.Dynamic, true);
        fa.name = fileName + " SDF";
        AssetDatabase.CreateAsset(fa, assetPath);
        fa.atlasTextures[0].name = fileName + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fileName + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);

        fa.TryAddCharacters(Chars, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"{fileName}: немає символів «{missing}»");
        EditorUtility.SetDirty(fa);
        return fa;
    }

    // ---------- Стиль ----------

    private static void StyleTree(Transform root, TMP_FontAsset font)
    {
        foreach (Text t in root.GetComponentsInChildren<Text>(true)) ConvertText(t, font);
        foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true)) t.font = font;
        foreach (Image img in root.GetComponentsInChildren<Image>(true)) StyleImage(img);
    }

    private static void ConvertText(Text t, TMP_FontAsset font)
    {
        GameObject go = t.gameObject;
        string text = t.text;
        float size = t.fontSize;
        Color color = t.color;
        TextAnchor anchor = t.alignment;
        FontStyle style = t.fontStyle;
        bool raycast = t.raycastTarget;

        Object.DestroyImmediate(t, true);       // на об'єкті може бути лише один Graphic

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = Map(anchor);
        tmp.fontStyle = style == FontStyle.Bold ? FontStyles.Bold
                      : style == FontStyle.Italic ? FontStyles.Italic
                      : style == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic
                      : FontStyles.Normal;
        tmp.raycastTarget = raycast;
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void StyleImage(Image img)
    {
        string n = img.gameObject.name;
        bool inSlider = img.GetComponentInParent<Slider>(true) != null;
        bool inToggle = img.GetComponentInParent<Toggle>(true) != null;

        if (n.StartsWith("Slot_")) Set(img, slot);
        else if (n == "Panel" || n == "Box") Set(img, panel);
        else if (inSlider && n == "Background") Set(img, slot);
        else if (inSlider && n == "Fill") { img.sprite = null; img.color = Gold; }
        else if (inSlider && n == "Handle") { img.sprite = null; img.color = new Color(0.93f, 0.88f, 0.78f, 1f); }
        else if (inToggle && n == "Background") Set(img, slot);
        else if (inToggle && n == "Checkmark") img.color = Gold;
        else if (img.GetComponent<Button>() != null) Set(img, button);
    }

    private static void Set(Image img, Sprite sprite)
    {
        if (sprite == null) return;
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;    // колір уже намальований у спрайті
    }

    // Поля, що раніше показували на Legacy Text, тепер мають тип TMP_Text — призначаємо заново
    private static void FixReferences(GameObject root)
    {
        var dialogue = root.GetComponent<UI_Dialogue>();
        if (dialogue != null)
        {
            var so = new SerializedObject(dialogue);
            so.FindProperty("nameText").objectReferenceValue = FindTmp(root.transform, "Name");
            so.FindProperty("bodyText").objectReferenceValue = FindTmp(root.transform, "Body");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var puzzle = root.GetComponent<ThreadPuzzleUI>();
        if (puzzle != null)
        {
            var so = new SerializedObject(puzzle);
            so.FindProperty("title").objectReferenceValue = FindTmp(root.transform, "Title");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var bag = root.GetComponent<UI_Bag>();
        if (bag != null)
        {
            var so = new SerializedObject(bag);
            so.FindProperty("slotColor").colorValue = Color.white;
            so.FindProperty("selectedColor").colorValue = new Color(1f, 0.82f, 0.45f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static TMP_Text FindTmp(Transform root, string name)
    {
        foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
            if (t.gameObject.name == name) return t;
        Debug.LogWarning($"{root.name}: не знайдено текст «{name}»");
        return null;
    }

    // Фон Площі Пошти за кнопками головного меню: на всю висоту, лівим краєм до лівого краю екрана
    private static void AddMenuBackdrop()
    {
        var menu = Object.FindAnyObjectByType<MainMenu>();
        var bg = AssetDatabase.LoadAssetAtPath<Sprite>(MenuBg);
        if (menu == null || bg == null || menu.transform.Find("Backdrop") != null) return;

        var go = new GameObject("Backdrop", typeof(RectTransform));
        go.transform.SetParent(menu.transform, false);
        go.transform.SetAsFirstSibling();
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite = bg;
        img.raycastTarget = false;
        var fit = go.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fit.aspectRatio = bg.rect.width / bg.rect.height;
    }

    // ---------- Дрібниці ----------

    private static Sprite ImportSprite(string path, int border)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("Немає " + path); return null; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100;
        ti.spriteBorder = new Vector4(border, border, border, border);
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TextAlignmentOptions Map(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
}
#endif
