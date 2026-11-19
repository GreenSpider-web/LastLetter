#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Задача 22: меню LastLetter → Task 22: Main menu and pause (спершу — Task 12, 13, 14, 21).
// Створює сцену MainMenu (меню, «Як грати», «Налаштування»), префаб UI_PauseMenu,
// додає паузу в Sandbox_Player і ставить MainMenu першою в Build Settings.
public static class Task22MenuSetup
{
    private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string SandboxPath = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string GameStatePrefab = "Assets/_Project/Prefabs/Managers/GameState.prefab";
    private const string InventoryPrefab = "Assets/_Project/Prefabs/Managers/InventoryManager.prefab";
    private const string SaveSystemPrefab = "Assets/_Project/Prefabs/Managers/SaveSystem.prefab";
    private const string PausePrefab = "Assets/_Project/Prefabs/UI/UI_PauseMenu.prefab";

    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
    private static readonly Color BoxColor = new Color(0.1f, 0.08f, 0.07f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.3f, 0.25f, 0.2f, 1f);
    private static readonly Color TextColor = new Color(0.93f, 0.9f, 0.84f);

    private const string HowToText =
        "ЛКМ — іти; по предмету — підійти й узяти, використати або поговорити\n" +
        "ПКМ — оглянути предмет; з предметом у руці — покласти його назад у сумку\n" +
        "Tab — сумка: клік по предмету — взяти в руку, по іншому — поєднати\n" +
        "Клік / Space / Enter — далі в діалозі\n" +
        "Esc — пауза\n\n" +
        "Листи можна прочитати над парою, але кожне читання має наслідки.\n" +
        "Гра зберігається сама, коли ви заходите в нову ділянку.";

    [MenuItem("LastLetter/Task 22: Main menu and pause")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SaveSystemPrefab) == null)
        {
            Debug.LogError("Спершу запусти Task 21: Add save to Sandbox_Player з меню LastLetter.");
            return;
        }

        EnsurePrefab<GameState>(GameStatePrefab, "GameState");

        BuildMainMenuScene();
        AddPauseToSandbox();
        SetBuildScenes();

        AssetDatabase.SaveAssets();
        Debug.Log("Задача 22: готово. Відкрий сцену MainMenu і натисни Play.");
    }

    private static void EnsurePrefab<T>(string path, string name) where T : Component
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var go = new GameObject(name);
        go.AddComponent<T>();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ---------- Сцена MainMenu ----------

    private static void BuildMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.1f, 0.08f);

        // менеджери живуть між сценами — у меню вони створюються вперше
        Spawn(GameStatePrefab);
        Spawn(InventoryPrefab);
        Spawn(SaveSystemPrefab);
        AddEventSystem();

        GameObject canvas = NewCanvas("UI_MainMenu", 10);

        Text title = NewText("Title", canvas.transform, 84, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.text = "Остання адреса";
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1400f, 140f));

        GameObject column = NewObject("Buttons", canvas.transform);
        Place(column.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(480f, 5 * 80f));
        AddVertical(column, 10f);

        Button cont = NewButton(column.transform, "Продовжити");
        Button newGame = NewButton(column.transform, "Нова гра");
        Button howTo = NewButton(column.transform, "Як грати");
        Button settings = NewButton(column.transform, "Налаштування");
        Button quit = NewButton(column.transform, "Вихід");

        GameObject howToPanel = BuildHowToPanel(canvas.transform);
        GameObject settingsPanel = BuildSettingsPanel(canvas.transform);

        var menu = canvas.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("continueButton").objectReferenceValue = cont;
        so.FindProperty("newGameButton").objectReferenceValue = newGame;
        so.FindProperty("howToPlayButton").objectReferenceValue = howTo;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("howToPlayPanel").objectReferenceValue = howToPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ---------- Пауза в Sandbox_Player ----------

    private static void AddPauseToSandbox()
    {
        var scene = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);

        if (Object.FindAnyObjectByType<SaveSystem>() == null) Spawn(SaveSystemPrefab);
        if (Object.FindAnyObjectByType<GameState>() == null) Spawn(GameStatePrefab);

        GameObject old = GameObject.Find("UI_PauseMenu");
        if (old != null) Object.DestroyImmediate(old);

        GameObject canvas = NewCanvas("UI_PauseMenu", 50);   // над сумкою (10) і діалогом (20)

        GameObject pausePanel = NewObject("PausePanel", canvas.transform);
        Stretch(pausePanel.GetComponent<RectTransform>());
        pausePanel.AddComponent<Image>().color = Dim;         // затемнення й блок кліків по сцені

        GameObject box = NewObject("Box", pausePanel.transform);
        Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 620f));
        box.AddComponent<Image>().color = BoxColor;
        AddVertical(box, 12f, 40);

        Text title = NewText("Title", box.transform, 48, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.text = "Пауза";
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;

        Button resume = NewButton(box.transform, "Продовжити");
        Button howTo = NewButton(box.transform, "Як грати");
        Button settings = NewButton(box.transform, "Налаштування");
        Button menuBtn = NewButton(box.transform, "Головне меню");
        Button quit = NewButton(box.transform, "Вихід");

        GameObject howToPanel = BuildHowToPanel(canvas.transform);
        GameObject settingsPanel = BuildSettingsPanel(canvas.transform);

        var pause = canvas.AddComponent<PauseMenu>();
        var so = new SerializedObject(pause);
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("howToPlayPanel").objectReferenceValue = howToPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("howToPlayButton").objectReferenceValue = howTo;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("menuButton").objectReferenceValue = menuBtn;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        pausePanel.SetActive(false);
        Directory.CreateDirectory(Path.GetDirectoryName(PausePrefab));
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, PausePrefab, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------- Вікна «Як грати» і «Налаштування» ----------

    private static GameObject BuildHowToPanel(Transform parent)
    {
        GameObject panel = ModalPanel("HowToPlayPanel", parent, new Vector2(1200f, 720f), "Як грати", out GameObject box);

        Text body = NewText("Body", box.transform, 30, FontStyle.Normal, TextAnchor.UpperLeft);
        body.text = HowToText;
        body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        AddBackButton(box.transform, panel);
        panel.SetActive(false);
        return panel;
    }

    private static GameObject BuildSettingsPanel(Transform parent)
    {
        GameObject panel = ModalPanel("SettingsPanel", parent, new Vector2(1000f, 620f), "Налаштування", out GameObject box);

        Slider master = SliderRow(box.transform, "Загальна гучність");
        Slider music = SliderRow(box.transform, "Музика");
        Slider sfx = SliderRow(box.transform, "Звуки");

        GameObject toggleGo = DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleGo.name = "Fullscreen";
        toggleGo.transform.SetParent(box.transform, false);
        toggleGo.AddComponent<LayoutElement>().preferredHeight = 50f;
        Text toggleLabel = toggleGo.GetComponentInChildren<Text>();
        StyleText(toggleLabel, 30, FontStyle.Normal, TextAnchor.MiddleLeft);
        toggleLabel.text = "Повний екран";
        toggleLabel.rectTransform.offsetMin = new Vector2(40f, 0f);

        // порожнє місце, щоб «Назад» був унизу
        NewObject("Space", box.transform).AddComponent<LayoutElement>().flexibleHeight = 1f;
        AddBackButton(box.transform, panel);

        var menu = panel.AddComponent<SettingsMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("masterSlider").objectReferenceValue = master;
        so.FindProperty("musicSlider").objectReferenceValue = music;
        so.FindProperty("sfxSlider").objectReferenceValue = sfx;
        so.FindProperty("fullscreenToggle").objectReferenceValue = toggleGo.GetComponent<Toggle>();
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return panel;
    }

    // Затемнення на весь екран + коробка з заголовком; вміст додається в box
    private static GameObject ModalPanel(string name, Transform parent, Vector2 size, string titleText, out GameObject box)
    {
        GameObject panel = NewObject(name, parent);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = Dim;

        box = NewObject("Box", panel.transform);
        Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        box.AddComponent<Image>().color = BoxColor;
        AddVertical(box, 16f, 40);

        Text title = NewText("Title", box.transform, 48, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.text = titleText;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
        return panel;
    }

    private static Slider SliderRow(Transform parent, string label)
    {
        GameObject row = NewObject(label, parent);
        row.AddComponent<LayoutElement>().preferredHeight = 60f;
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 20f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        Text text = NewText("Label", row.transform, 30, FontStyle.Normal, TextAnchor.MiddleLeft);
        text.text = label;
        var le = text.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 340f;
        le.preferredHeight = 60f;

        GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.transform.SetParent(row.transform, false);
        var sle = sliderGo.AddComponent<LayoutElement>();
        sle.flexibleWidth = 1f;
        sle.preferredHeight = 24f;
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static void AddBackButton(Transform parent, GameObject panel)
    {
        Button back = NewButton(parent, "Назад");
        UnityEventTools.AddBoolPersistentListener(back.onClick, panel.SetActive, false);
    }

    // ---------- Дрібні помічники ----------

    private static void Spawn(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null) PrefabUtility.InstantiatePrefab(prefab);
    }

    private static void AddEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject NewCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject NewObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text NewText(string name, Transform parent, int size, FontStyle style, TextAnchor anchor)
    {
        var t = NewObject(name, parent).AddComponent<Text>();
        StyleText(t, size, style, anchor);
        return t;
    }

    private static void StyleText(Text t, int size, FontStyle style, TextAnchor anchor)
    {
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = TextColor;
        t.raycastTarget = false;
    }

    private static Button NewButton(Transform parent, string label)
    {
        GameObject go = NewObject("Button_" + label, parent);
        go.AddComponent<Image>().color = ButtonColor;
        go.AddComponent<LayoutElement>().preferredHeight = 70f;
        var button = go.AddComponent<Button>();

        Text t = NewText("Label", go.transform, 32, FontStyle.Normal, TextAnchor.MiddleCenter);
        t.text = label;
        Stretch(t.rectTransform);
        return button;
    }

    private static void AddVertical(GameObject go, float spacing, int padding = 0)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = new RectOffset(padding, padding, padding, padding);
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
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

    // MainMenu — сцена 0 (з неї стартує білд), далі пісочниця
    private static void SetBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == MenuScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
        if (!scenes.Exists(s => s.path == SandboxPath))
            scenes.Add(new EditorBuildSettingsScene(SandboxPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
