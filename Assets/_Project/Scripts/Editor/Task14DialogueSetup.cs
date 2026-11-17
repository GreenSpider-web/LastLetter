#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Задача 14: меню LastLetter → Task 14: Add dialogue to Sandbox_Player (спершу — Task 12 і Task 13).
// Додає GameState, DialogueManager + вікно діалогу UI_Dialogue і Пантелеймона біля ліхтаря.
// Ліхтар, запалений свічкою, ставить lantern_lit = true; розмова з Пантелеймоном читає lantern_lit і ставить prologue_done.
public static class Task14DialogueSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string InkPath = "Assets/_Project/Ink/LastLetter_Ch1.ink";
    private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items";
    private const string GameStatePrefab = "Assets/_Project/Prefabs/Managers/GameState.prefab";
    private const string DialoguePrefab = "Assets/_Project/Prefabs/UI/UI_Dialogue.prefab";
    private const string TalkCursorPath = "Assets/_Project/Art/UI/cursor_talk.png";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";

    [MenuItem("LastLetter/Task 14: Add dialogue to Sandbox_Player")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (GameObject.Find("Interaction") == null || GameObject.Find("InventoryManager") == null)
        {
            Debug.LogError("Спершу запусти Task 12 і Task 13 з меню LastLetter.");
            return;
        }

        // асети беремо після відкриття сцени
        var ink = AssetDatabase.LoadAssetAtPath<InkFile>(InkPath);
        if (ink == null || !ink.isCompiled)
        {
            Debug.LogError($"Не вдалося взяти скомпільований {InkPath}. Виділи його в Project і подивись помилки Ink в Inspector.");
            return;
        }

        // 1. Курсор «говорити» → CursorManager
        Texture2D talk = ImportCursor(TalkCursorPath);
        var cursors = Object.FindAnyObjectByType<CursorManager>();
        var cso = new SerializedObject(cursors);
        cso.FindProperty("talkCursor").objectReferenceValue = talk;
        cso.ApplyModifiedPropertiesWithoutUndo();

        // 2. GameState (якщо ще немає)
        if (Object.FindAnyObjectByType<GameState>() == null)
        {
            var gs = new GameObject("GameState");
            gs.AddComponent<GameState>();
            SavePrefab(gs, GameStatePrefab);
        }

        // 3. Вікно діалогу + DialogueManager на одному об'єкті
        Replace("UI_Dialogue");
        GameObject root = BuildDialogueUI(out UI_Dialogue view);
        var manager = root.AddComponent<DialogueManager>();
        var mso = new SerializedObject(manager);
        mso.FindProperty("inkFile").objectReferenceValue = ink;
        mso.FindProperty("view").objectReferenceValue = view;
        FillSpeakers(mso.FindProperty("speakers"));
        FillItems(mso.FindProperty("items"));
        mso.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, DialoguePrefab);

        // 4. Ліхтар: використали Запалену свічку → lantern_lit = true
        GameObject lamp = GameObject.Find("Use_Lamp");
        if (lamp != null && lamp.GetComponent<GameFlagSetter>() == null)
        {
            var setter = lamp.AddComponent<GameFlagSetter>();      // за замовчуванням lantern_lit = true
            var it = lamp.GetComponent<Interactable>();
            UnityEventTools.AddPersistentListener(it.onItemUsed, setter.Apply);
            EditorUtility.SetDirty(it);
        }

        // 5. Пантелеймон біля ліхтаря (сірий прямокутник)
        Replace("NPC_Panteleimon");
        var npc = new GameObject("NPC_Panteleimon");
        npc.transform.position = new Vector3(11f, -2f, 0f);
        npc.transform.localScale = new Vector3(0.8f, 2.2f, 1f);
        var sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        sr.color = new Color(0.45f, 0.5f, 0.55f);
        npc.AddComponent<BoxCollider2D>();

        var iso = new SerializedObject(npc.AddComponent<Interactable>());
        iso.FindProperty("displayName").stringValue = "Пантелеймон";
        iso.FindProperty("description").stringValue = "Старий рибалка з вудкою. Річка давно висохла.";
        iso.FindProperty("type").enumValueIndex = (int)InteractionType.Talk;
        iso.ApplyModifiedPropertiesWithoutUndo();

        // У грі цей вузол запускає доставка листа 1 (LetterManager, задача 15); тут — одразу розмовою
        var dso = new SerializedObject(npc.AddComponent<NPCDialogue>());
        dso.FindProperty("knot").stringValue = "panteleimon_letter";
        dso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Задача 14: діалоги додано. Play → поговори з Пантелеймоном, потім запали ліхтар свічкою і поговори ще раз.");
    }

    private static void FillSpeakers(SerializedProperty list)
    {
        // id — як у тегах #speaker у LastLetter_Ch1.ink; narrator і addressee без імені
        var data = new List<(string id, string name, Color color)>
        {
            ("lilia",          "Ліля",                new Color(0.72f, 0.83f, 0.96f)),
            ("marka",          "Марка",               new Color(0.95f, 0.85f, 0.6f)),
            ("postmaster",     "Поштмейстер",         new Color(0.85f, 0.7f, 0.65f)),
            ("panteleimon",    "Пантелеймон",         new Color(0.75f, 0.85f, 0.8f)),
            ("stefania",       "Пані Стефанія",       new Color(0.9f, 0.75f, 0.8f)),
            ("umbrella_woman", "Жінка з парасолькою", new Color(0.8f, 0.8f, 0.9f)),
            ("clockmaker",     "Годинникар",          new Color(0.85f, 0.8f, 0.65f)),
        };

        list.arraySize = data.Count;
        for (int i = 0; i < data.Count; i++)
        {
            SerializedProperty s = list.GetArrayElementAtIndex(i);
            s.FindPropertyRelative("id").stringValue = data[i].id;
            s.FindPropertyRelative("displayName").stringValue = data[i].name;
            s.FindPropertyRelative("nameColor").colorValue = data[i].color;
        }
    }

    private static void FillItems(SerializedProperty list)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemsDir });
        list.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guids[i]));
    }

    // Canvas → Window (портрет, панель з ім'ям і текстом, 4 кнопки варіантів)
    private static GameObject BuildDialogueUI(out UI_Dialogue view)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("UI_Dialogue", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;           // над сумкою
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        var window = UIObject("Window", root.transform);
        Stretch(window.GetComponent<RectTransform>(), 0f);

        // портрет зліва над панеллю
        var portraitObj = UIObject("Portrait", window.transform);
        var prt = portraitObj.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 0f);
        prt.anchoredPosition = new Vector2(60f, 300f);
        prt.sizeDelta = new Vector2(400f, 400f);
        var portrait = portraitObj.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        // панель унизу
        var panel = UIObject("Panel", window.transform);
        var prect = panel.GetComponent<RectTransform>();
        prect.anchorMin = new Vector2(0f, 0f);
        prect.anchorMax = new Vector2(1f, 0f);
        prect.pivot = new Vector2(0.5f, 0f);
        prect.offsetMin = new Vector2(40f, 30f);
        prect.offsetMax = new Vector2(-40f, 300f);
        panel.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.05f, 0.9f);
        var panelButton = panel.AddComponent<Button>();
        panelButton.transition = Selectable.Transition.None;

        Text nameText = TextObject("Name", panel.transform, font, 30, FontStyle.Bold, TextAnchor.UpperLeft);
        var nrt = nameText.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 1f);
        nrt.anchorMax = new Vector2(1f, 1f);
        nrt.pivot = new Vector2(0.5f, 1f);
        nrt.offsetMin = new Vector2(30f, -60f);
        nrt.offsetMax = new Vector2(-30f, -16f);

        Text bodyText = TextObject("Body", panel.transform, font, 28, FontStyle.Normal, TextAnchor.UpperLeft);
        var brt = bodyText.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(30f, 20f);
        brt.offsetMax = new Vector2(-30f, -66f);

        // варіанти відповіді — стовпчик над панеллю справа
        var choices = UIObject("Choices", window.transform);
        var crt = choices.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 0f);
        crt.anchoredPosition = new Vector2(-60f, 320f);
        crt.sizeDelta = new Vector2(900f, 4 * 70f + 3 * 10f);
        var layout = choices.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.LowerRight;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;

        var buttons = new Button[4];
        for (int i = 0; i < buttons.Length; i++)
        {
            var b = UIObject("Choice_" + i, choices.transform);
            b.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 70f);
            b.AddComponent<Image>().color = new Color(0.3f, 0.25f, 0.2f, 0.95f);
            buttons[i] = b.AddComponent<Button>();
            Text label = TextObject("Label", b.transform, font, 26, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(label.GetComponent<RectTransform>(), 16f);
        }

        view = root.AddComponent<UI_Dialogue>();
        var so = new SerializedObject(view);
        so.FindProperty("window").objectReferenceValue = window;
        so.FindProperty("panelButton").objectReferenceValue = panelButton;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("bodyText").objectReferenceValue = bodyText;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        var arr = so.FindProperty("choiceButtons");
        arr.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        window.SetActive(false);
        return root;
    }

    private static Text TextObject(string name, Transform parent, Font font, int size, FontStyle style, TextAnchor anchor)
    {
        var go = UIObject(name, parent);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = new Color(0.93f, 0.9f, 0.84f);
        t.raycastTarget = false;
        return t;
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform r, float pad)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(pad, pad);
        r.offsetMax = new Vector2(-pad, -pad);
    }

    private static Texture2D ImportCursor(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.Cursor)
        {
            ti.textureType = TextureImporterType.Cursor;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void SavePrefab(GameObject go, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
    }

    private static void Replace(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
    }
}
#endif
