#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Задача 23: меню LastLetter → Task 23: Thread puzzle (Sandbox_Threads).
// 1. Тестові візерунки Puzzle_Gate («Хвіртка» 3×3) і Puzzle_Hall («Коридор» 4×4) — лише якщо їх ще немає,
//    щоб не затерти візерунки Софії (задача 24).
// 2. Префаб UI_ThreadPuzzle (вікно пазла).
// 3. Сцена Sandbox_Threads: два блокери з нитками і тестові кнопки.
public static class Task23ThreadPuzzleSetup
{
    private const string PuzzlesDir = "Assets/_Project/ScriptableObjects/ThreadPuzzles";
    private const string GatePath = PuzzlesDir + "/Puzzle_Gate.asset";
    private const string HallPath = PuzzlesDir + "/Puzzle_Hall.asset";
    private const string UiPrefab = "Assets/_Project/Prefabs/UI/UI_ThreadPuzzle.prefab";
    private const string GameStatePrefab = "Assets/_Project/Prefabs/Managers/GameState.prefab";
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Threads.unity";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";

    [MenuItem("LastLetter/Task 23: Thread puzzle (Sandbox_Threads)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. Візерунки
        Directory.CreateDirectory(PuzzlesDir);
        ThreadPuzzleData gate = AssetDatabase.LoadAssetAtPath<ThreadPuzzleData>(GatePath);
        if (gate == null) gate = CreateGate();
        ThreadPuzzleData hall = AssetDatabase.LoadAssetAtPath<ThreadPuzzleData>(HallPath);
        if (hall == null) hall = CreateHall();
        Debug.Log($"«{gate.Title}»: мінімум ходів {gate.MinMoves()}; «{hall.Title}»: мінімум ходів {hall.MinMoves()} — записати в GDD");

        // 2. Префаб вікна
        GameObject uiRoot = BuildUI();
        Directory.CreateDirectory(Path.GetDirectoryName(UiPrefab));
        PrefabUtility.SaveAsPrefabAsset(uiRoot, UiPrefab);
        Object.DestroyImmediate(uiRoot);

        // 3. Сцена
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var gsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameStatePrefab);
        if (gsPrefab != null) PrefabUtility.InstantiatePrefab(gsPrefab);
        else new GameObject("GameState").AddComponent<GameState>();

        var ui = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefab)))
                 .GetComponent<ThreadPuzzleUI>();

        MakeBlocker("Blocker_Gate", gate, ui, new Vector3(-3f, 0f, 0f), new Color(0.55f, 0.2f, 0.2f));
        MakeBlocker("Blocker_Hall", hall, ui, new Vector3(3f, 0f, 0f), new Color(0.75f, 0.72f, 0.65f));

        new GameObject("SandboxThreadsClicker").AddComponent<SandboxThreadsClicker>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Задача 23: Sandbox_Threads готова. Play → «Відкрити «Хвіртка»».");
    }

    // ---------- Візерунки (тимчасові, Софія замінить у задачі 24) ----------

    // «Хвіртка» 3×3: червона нитка від голки зліва вгорі до котушки справа внизу. Мінімум 7 ходів
    private static ThreadPuzzleData CreateGate()
    {
        const ThreadTileType S = ThreadTileType.Straight, C = ThreadTileType.Corner, T = ThreadTileType.Tee;
        var tiles = new[]
        {
            Tile(S, 0, 1), Tile(C, 0, 2), Tile(T, 1),
            Tile(C, 3),    Tile(S, 1, 0), Tile(C, 2),
            Tile(S, 1),    Tile(C, 2, 0), Tile(S, 0, 1),
        };
        var lines = new[]
        {
            Line(ThreadColor.Red, 0, 0, ThreadSide.Left, 2, 2, ThreadSide.Right),
        };
        return CreatePuzzle(GatePath, "Хвіртка", "puzzle_gate_solved", 3, 3, tiles, lines);
    }

    // «Коридор» 4×4: червона і біла нитки перетинаються в хресті. Мінімум 17 ходів
    private static ThreadPuzzleData CreateHall()
    {
        const ThreadTileType S = ThreadTileType.Straight, C = ThreadTileType.Corner,
                             T = ThreadTileType.Tee, X = ThreadTileType.Cross;
        var tiles = new[]
        {
            Tile(T, 1),    Tile(S, 0),    Tile(S, 1, 0), Tile(C, 2),
            Tile(C, 3, 1), Tile(S, 0, 1), Tile(X, 0, 0), Tile(C, 0, 2),
            Tile(C, 0, 3), Tile(C, 3, 1), Tile(C, 1, 3), Tile(S, 1, 0),
            Tile(T, 2),    Tile(S, 1, 0), Tile(C, 1),    Tile(C, 2, 0),
        };
        var lines = new[]
        {
            Line(ThreadColor.Red, 0, 2, ThreadSide.Left, 3, 3, ThreadSide.Right),
            Line(ThreadColor.White, 2, 0, ThreadSide.Up, 1, 3, ThreadSide.Down),
        };
        return CreatePuzzle(HallPath, "Коридор", "puzzle_hall_solved", 4, 4, tiles, lines);
    }

    private static ThreadTile Tile(ThreadTileType type, int start, int solution = -1) =>
        new ThreadTile { type = type, startRotation = start, solutionRotation = solution };

    private static ThreadLine Line(ThreadColor color, int nx, int ny, ThreadSide nSide, int sx, int sy, ThreadSide sSide) =>
        new ThreadLine
        {
            color = color,
            needleCell = new Vector2Int(nx, ny), needleSide = nSide,
            spoolCell = new Vector2Int(sx, sy), spoolSide = sSide
        };

    private static ThreadPuzzleData CreatePuzzle(string path, string title, string flag, int w, int h,
                                                 ThreadTile[] tiles, ThreadLine[] lines)
    {
        var data = ScriptableObject.CreateInstance<ThreadPuzzleData>();
        AssetDatabase.CreateAsset(data, path);

        var so = new SerializedObject(data);
        so.FindProperty("title").stringValue = title;
        so.FindProperty("solvedFlag").stringValue = flag;
        so.FindProperty("width").intValue = w;
        so.FindProperty("height").intValue = h;

        SerializedProperty tp = so.FindProperty("tiles");
        tp.arraySize = tiles.Length;
        for (int i = 0; i < tiles.Length; i++)
        {
            SerializedProperty e = tp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("type").enumValueIndex = (int)tiles[i].type;
            e.FindPropertyRelative("startRotation").intValue = tiles[i].startRotation;
            e.FindPropertyRelative("solutionRotation").intValue = tiles[i].solutionRotation;
        }

        SerializedProperty lp = so.FindProperty("lines");
        lp.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            SerializedProperty e = lp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("color").enumValueIndex = (int)lines[i].color;
            e.FindPropertyRelative("needleCell").vector2IntValue = lines[i].needleCell;
            e.FindPropertyRelative("needleSide").enumValueIndex = (int)lines[i].needleSide;
            e.FindPropertyRelative("spoolCell").vector2IntValue = lines[i].spoolCell;
            e.FindPropertyRelative("spoolSide").enumValueIndex = (int)lines[i].spoolSide;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    // ---------- Вікно пазла ----------

    private static GameObject BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("UI_ThreadPuzzle", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;           // над сумкою, під діалогом (20) — щоб підказка Марки була зверху
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        // затемнення на весь екран (і блок кліків по сцені)
        var window = UIObject("Window", root.transform);
        Stretch(window);
        window.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

        Text title = AddText("Title", window, font, 44, TextAnchor.UpperCenter);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        title.rectTransform.sizeDelta = new Vector2(0f, 60f);

        Text help = AddText("Help", window, font, 26, TextAnchor.LowerCenter);
        help.text = "Клік по плитці — повернути. Доведи кожну нитку від голки до її котушки. ПКМ — вийти.";
        help.rectTransform.anchorMin = new Vector2(0f, 0f);
        help.rectTransform.anchorMax = new Vector2(1f, 0f);
        help.rectTransform.pivot = new Vector2(0.5f, 0f);
        help.rectTransform.anchoredPosition = new Vector2(0f, 60f);
        help.rectTransform.sizeDelta = new Vector2(0f, 40f);

        // сітка по центру; розмір ставить ThreadPuzzleUI
        RectTransform board = UIObject("Board", window);

        // кнопка «Закрити» праворуч угорі
        RectTransform close = UIObject("CloseButton", window);
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 1f);
        close.anchoredPosition = new Vector2(-40f, -40f);
        close.sizeDelta = new Vector2(200f, 64f);
        close.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.25f, 0.2f);
        Button closeButton = close.gameObject.AddComponent<Button>();
        Text closeText = AddText("Label", close, font, 28, TextAnchor.MiddleCenter);
        closeText.text = "Закрити";
        Stretch(closeText.rectTransform);

        var ui = root.AddComponent<ThreadPuzzleUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("window").objectReferenceValue = window.gameObject;
        so.FindProperty("board").objectReferenceValue = board;
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        window.gameObject.SetActive(false);
        return root;
    }

    // Блокер у сцені: прямокутник «з ниток», клік мишею в грі відкриває пазл через Interactable
    private static void MakeBlocker(string name, ThreadPuzzleData data, ThreadPuzzleUI ui, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 3f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        sr.color = color;
        go.AddComponent<BoxCollider2D>();

        var it = new SerializedObject(go.AddComponent<Interactable>());
        it.FindProperty("displayName").stringValue = "Нитки: " + data.Title;
        it.FindProperty("description").stringValue = "Вхід обплутаний нитками. Їх можна розплутати.";
        it.FindProperty("type").enumValueIndex = (int)InteractionType.Use;
        it.FindProperty("useText").stringValue = "Нитки натягнуті туго.";
        it.ApplyModifiedPropertiesWithoutUndo();

        var trigger = new SerializedObject(go.AddComponent<ThreadPuzzleTrigger>());
        trigger.FindProperty("data").objectReferenceValue = data;
        trigger.FindProperty("puzzleUI").objectReferenceValue = ui;
        trigger.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform UIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Text AddText(string name, Transform parent, Font font, int size, TextAnchor anchor)
    {
        Text t = UIObject(name, parent).gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = new Color(0.93f, 0.9f, 0.84f);
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
#endif
