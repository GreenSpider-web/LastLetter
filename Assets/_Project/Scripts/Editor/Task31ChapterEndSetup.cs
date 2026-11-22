#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Задача 31: меню LastLetter →
//  • Task 31: Add chapter end screens — збирає префаб UI_ChapterEnd (екрани «Главу 1 завершено» і «Межа пам'ятає»)
//    і кладе його у відкриту ігрову сцену;
//  • Task 31: Print demo summaries — виводить у Console підсумок для шляхів А і Б з Втручанням 0, 1, 2.
public static class Task31ChapterEndSetup
{
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/UI_ChapterEnd.prefab";
    private const string PanelPath = "Assets/_Project/Art/UI/ui_panel.png";
    private const string ButtonPath = "Assets/_Project/Art/UI/ui_button.png";
    private const string MezhaBg = "Assets/_Project/Art/Backgrounds/bg_mezha_no_death.png";
    private const string GiftA = "Assets/_Project/ScriptableObjects/Items/item_red_thread.asset";
    private const string GiftB = "Assets/_Project/ScriptableObjects/Items/item_thimble.asset";

    private static readonly Color TextColor = new Color(0.93f, 0.9f, 0.84f);
    private static readonly Color MarkaColor = new Color(0.95f, 0.85f, 0.6f);

    [MenuItem("LastLetter/Task 31: Add chapter end screens")]
    public static void Build()
    {
        if (Object.FindAnyObjectByType<PlayerController>() == null)
        {
            Debug.LogError("Відкрий ігрову сцену (Sandbox_Player або сцену глави 1), потім запусти меню.");
            return;
        }

        GameObject old = GameObject.Find("UI_ChapterEnd");
        if (old != null) Object.DestroyImmediate(old);

        Sprite panel = LoadSprite(PanelPath);
        Sprite button = LoadSprite(ButtonPath);
        Sprite mezha = LoadSprite(MezhaBg);

        // Canvas поверх усього (сумка 10, пазл 15, діалог 20, пауза 50)
        var root = new GameObject("UI_ChapterEnd", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        // ---------- Екран 10: Главу 1 завершено ----------
        RectTransform chapter = Child("ChapterScreen", root.transform);
        Stretch(chapter);
        chapter.gameObject.AddComponent<Image>().color = Color.black;
        var chapterGroup = chapter.gameObject.AddComponent<CanvasGroup>();

        TMP_Text title = Text("Title", chapter, 76, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1400f, 110f));
        title.text = "Главу 1 завершено";

        TMP_Text sub = Text("Subtitle", chapter, 38, FontStyles.Italic, TextAlignmentOptions.Center, TextColor);
        Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1400f, 70f));
        sub.text = "Пані Стефанія перейшла міст.";

        Button next = MakeButton("NextButton", chapter, "Далі", button);
        Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(280f, 72f));

        // ---------- Екран 11: Межа пам'ятає ----------
        RectTransform demo = Child("DemoEndScreen", root.transform);
        Stretch(demo);
        demo.gameObject.AddComponent<Image>().color = Color.black;
        var demoGroup = demo.gameObject.AddComponent<CanvasGroup>();

        if (mezha != null)
        {
            // фон Межі на весь екран, без чорних смуг
            RectTransform bg = Child("Backdrop", demo);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = mezha;
            bgImg.raycastTarget = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = mezha.rect.width / mezha.rect.height;

            RectTransform shade = Child("Shade", demo);
            Stretch(shade);
            var shadeImg = shade.gameObject.AddComponent<Image>();
            shadeImg.color = new Color(0f, 0f, 0f, 0.45f);
            shadeImg.raycastTarget = false;
        }

        RectTransform box = Child("Box", demo);
        Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 780f));
        var boxImg = box.gameObject.AddComponent<Image>();
        boxImg.sprite = panel;
        boxImg.type = Image.Type.Sliced;

        TMP_Text demoTitle = Text("Title", box, 64, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
        Place(demoTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1000f, 90f));
        demoTitle.text = "Межа пам'ятає";

        TMP_Text summary = Text("Summary", box, 32, FontStyles.Normal, TextAlignmentOptions.TopLeft, TextColor);
        Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1000f, 400f));
        summary.lineSpacing = 12f;

        TMP_Text marka = Text("Marka", box, 30, FontStyles.Italic, TextAlignmentOptions.Center, MarkaColor);
        Place(marka.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1000f, 60f));

        Button menu = MakeButton("MenuButton", box, "Головне меню", button);
        Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(-170f, 50f), new Vector2(300f, 72f));
        Button quit = MakeButton("QuitButton", box, "Вийти", button);
        Place((RectTransform)quit.transform, new Vector2(0.5f, 0f), new Vector2(170f, 50f), new Vector2(300f, 72f));

        // ---------- Скрипт ----------
        var ui = root.AddComponent<UI_ChapterEnd>();
        var so = new SerializedObject(ui);
        so.FindProperty("chapterScreen").objectReferenceValue = chapterGroup;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("demoEndScreen").objectReferenceValue = demoGroup;
        so.FindProperty("summaryText").objectReferenceValue = summary;
        so.FindProperty("markaText").objectReferenceValue = marka;
        so.FindProperty("menuButton").objectReferenceValue = menu;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("giftPathA").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(GiftA);
        so.FindProperty("giftPathB").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(GiftB);
        so.ApplyModifiedPropertiesWithoutUndo();

        chapter.gameObject.SetActive(false);
        demo.gameObject.SetActive(false);

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Задача 31: UI_ChapterEnd додано в сцену. Збережи сцену (Ctrl+S).");
    }

    [MenuItem("LastLetter/Task 31: Print demo summaries")]
    public static void PrintSummaries()
    {
        // шлях А: лист рибалці міг бути прочитаний (0 або 1), лист кравчині — ні (інакше це був би шлях Б)
        // шлях Б: лист кравчині прочитано, лист рибалці — так чи ні (Втручання 1 або 2)
        Log("Шлях А, Втручання 0", false, false, false);
        Log("Шлях А, Втручання 1", true, false, false);
        Log("Шлях А, Втручання 2 (прочитала обидва, але віддала лист як є)", true, true, false);
        Log("Шлях Б, Втручання 1", false, true, true);
        Log("Шлях Б, Втручання 2", true, true, true);
    }

    private static void Log(string caseName, bool readFisher, bool readTailor, bool pathB)
    {
        var r = new UI_ChapterEnd.Result
        {
            delivered = 2,
            totalLetters = 2,
            readFisher = readFisher,
            readTailor = readTailor,
            intervention = (readFisher ? 1 : 0) + (readTailor ? 1 : 0),
            hasGiftA = !pathB,
            hasGiftB = pathB,
            panteleimonLeft = true,
            stefaniaLeft = true,
        };
        Debug.Log(caseName + ":\n" + UI_ChapterEnd.BuildSummary(r));
    }

    // ---------- Помічники ----------

    private static Sprite LoadSprite(string path)
    {
        Sprite best = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s && (best == null || s.rect.width * s.rect.height > best.rect.width * best.rect.height))
                best = s;
        return best;
    }

    private static RectTransform Child(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static TMP_Text Text(string name, Transform parent, float size, FontStyles style,
                                 TextAlignmentOptions align, Color color)
    {
        var t = Child(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static Button MakeButton(string name, Transform parent, string label, Sprite sprite)
    {
        RectTransform r = Child(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        var b = r.gameObject.AddComponent<Button>();
        TMP_Text t = Text("Label", r, 32, FontStyles.Normal, TextAlignmentOptions.Center, TextColor);
        Stretch(t.rectTransform);
        t.text = label;
        return b;
    }

    private static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
#endif
