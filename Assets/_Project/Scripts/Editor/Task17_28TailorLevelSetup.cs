#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Задачі 17 і 28: рівень «Кравчиня» — ділянки 1-Б Вулиця Ниток і 1-В Дім Кравчині (GDD → «Дизайн рівнів»).
// Меню LastLetter → «Task 17-28: Зібрати рівень Кравчині»:
//  1. Сцени L1_TailorStreet і L1_TailorHouse — копії Sandbox_Player (менеджери, Ліля з Маркою, камера, UI),
//     з яких прибрано все з Площі Пошти, і додано фон, підлогу, NPC, предмети, нитки й двері за GDD.
//  2. У Sandbox_Player (поки замість L1_PostSquare) — арка на Вулицю Ниток, відчиняється після пролога (prologue_done).
//  3. Обидві сцени — у Build Settings.
// NPC без арту — сірі прямокутники-заглушки (замінити спрайтами, коли Софія намалює).
// Координати x — як у GDD: від лівого краю фону, 1 од. = 1 клітинка; фон заввишки 9 од.
// Запустити ще раз можна — сцени перебудуються з нуля.
public static class Task17_28TailorLevelSetup
{
    private const string SourceScene = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string StreetScene = "Assets/_Project/Scenes/L1_TailorStreet.unity";
    private const string HouseScene = "Assets/_Project/Scenes/L1_TailorHouse.unity";

    private const string StreetBg = "Assets/_Project/Art/Backgrounds/L1_TailorStreet.PNG";
    private const string HouseBg = "Assets/_Project/Art/Backgrounds/L1_TailorHouse.JPG";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";
    private const string PuzzleUiPrefab = "Assets/_Project/Prefabs/UI/UI_ThreadPuzzle.prefab";
    private const string ChapterEndPrefab = "Assets/_Project/Prefabs/UI/UI_ChapterEnd.prefab";
    private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items/";
    private const string PuzzlesDir = "Assets/_Project/ScriptableObjects/ThreadPuzzles/";

    // Що з Площі Пошти не потрібне на інших ділянках (кореневі об'єкти Sandbox_Player)
    private static readonly string[] PostSquareObjects =
    {
        "Item_Candle", "Item_Matches", "Item_Cube", "NPC_Panteleimon", "NPC_Postmaster", "postmaster_idle 1_0",
        "Examine_Poster", "Use_Kettle", "Use_Lamp", "background1_0", "WalkArea", "Door_TailorStreet"
    };

    // Підлога (смуга бруківки / дощок), y — від верху до низу
    private const float StreetFloorTop = -2.9f, StreetFloorBottom = -4.3f;
    private const float HouseFloorTop = -2.5f, HouseFloorBottom = -4.3f;
    private const float NpcY = -2.2f;   // центр NPC-заглушки 0.8×2.2 — ноги на бруківці

    private static readonly Color ThreadColor = new Color(0.8f, 0.15f, 0.15f, 0.45f);

    [MenuItem("LastLetter/Task 17-28: Зібрати рівень Кравчині")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene) == null)
        {
            Debug.LogError("Немає " + SourceScene);
            return;
        }

        BuildStreet();
        BuildHouse();
        AddArchToPostSquare();
        AddToBuildSettings(StreetScene, HouseScene);

        EditorSceneManager.OpenScene(StreetScene);
        Debug.Log("Рівень Кравчині зібрано: L1_TailorStreet і L1_TailorHouse. Арка в Sandbox_Player відчиняється після пролога.");
    }

    // ---------- 1-Б Вулиця Ниток (48×9) ----------
    private static void BuildStreet()
    {
        Scene scene = CopySourceScene(StreetScene);
        float width = Background(StreetBg);

        PolygonCollider2D beforeGate = WalkArea("WalkArea_BeforeGate", 0.6f, 37f, StreetFloorTop, StreetFloorBottom);
        PolygonCollider2D full = WalkArea("WalkArea_Full", 0.6f, width - 1f, StreetFloorTop, StreetFloorBottom);
        PlayerController player = SetupPlayerAndCamera(beforeGate, new Vector2(3f, -3.5f), width);
        FlagWalk("puzzle_gate_solved", player, beforeGate, full);

        // арка назад на Площу Пошти (x = 1)
        Door("Door_PostSquare", "Арка на Площу Пошти", "Звідси я прийшла.", new Vector2(2.2f, -0.6f), new Vector2(3f, 4.4f),
             "Sandbox_Player", new Vector2(9.8f, -3.2f), "", "");

        // мешканці з підказками
        Npc("NPC_UmbrellaWoman", "Жінка з парасолькою", "Парасолька розкрита, хоча дощу немає.", "umbrella_woman", 8f,
            new Color(0.45f, 0.4f, 0.55f));
        Npc("NPC_Clockmaker", "Годинникар", "Старий із лупою в оці. За ним — вітрина з годинниками без стрілок.", "clockmaker", 24.9f,
            new Color(0.5f, 0.42f, 0.3f));

        // чужі двері (x ≈ 12, 20, 30 — де двері на фоні)
        foreach (float x in new[] { 11.6f, 19.5f, 27.9f })
            Examine("Examine_Door_" + Mathf.RoundToInt(x), "Чужі двері", "Зачинено. Тут ніхто не чекає листів.",
                    new Vector2(x, -1f), new Vector2(1f, 2.4f));

        // предмети
        Pickup("Item_Scissors", "Ножиці", Item("item_scissors"), new Vector2(16f, -3.4f), new Vector2(0.6f, 0.4f),
               new Color(0.75f, 0.78f, 0.8f));
        Pickup("Item_Button", "Ґудзик", Item("item_button"), new Vector2(36f, -3.8f), new Vector2(0.4f, 0.4f),
               new Color(0.95f, 0.93f, 0.88f));

        // хвіртка з нитками (x = 38) і двері кравчині (x = 41)
        ThreadPuzzleUI ui = InstantiatePrefab(PuzzleUiPrefab).GetComponentInChildren<ThreadPuzzleUI>(true);
        ThreadBlocker("ThreadPuzzle_Gate", Puzzle("Puzzle_Gate"), ui, new Vector2(38.4f, -2.6f), new Vector2(2.4f, 3.4f));
        Door("Door_TailorHouse", "Двері кравчині", "Вікно поруч світиться, хоча сонце давно сіло.",
             new Vector2(41.7f, -1.15f), new Vector2(1f, 2.1f),
             "L1_TailorHouse", new Vector2(2.6f, -3.4f), "puzzle_gate_solved", "Хвіртку обплутали нитки.");

        EditorSceneManager.SaveScene(scene);
    }

    // ---------- 1-В Дім Кравчині (16×9) ----------
    private static void BuildHouse()
    {
        Scene scene = CopySourceScene(HouseScene);
        float width = Background(HouseBg);

        PolygonCollider2D beforeHall = WalkArea("WalkArea_BeforeHall", 0.6f, 3.7f, HouseFloorTop, HouseFloorBottom);
        PolygonCollider2D full = WalkArea("WalkArea_Full", 0.6f, width - 0.6f, HouseFloorTop, HouseFloorBottom);
        PlayerController player = SetupPlayerAndCamera(beforeHall, new Vector2(2.6f, -3.4f), width);
        FlagWalk("puzzle_hall_solved", player, beforeHall, full);

        // вихід на вулицю (x = 1)
        Door("Door_Street", "Двері на вулицю", "Назад на Вулицю Ниток.", new Vector2(1.35f, 0.45f), new Vector2(1.6f, 5.7f),
             "L1_TailorStreet", new Vector2(41f, -3.5f), "", "");

        // коридор із нитками (x = 4)
        ThreadPuzzleUI ui = InstantiatePrefab(PuzzleUiPrefab).GetComponentInChildren<ThreadPuzzleUI>(true);
        ThreadBlocker("ThreadPuzzle_Hall", Puzzle("Puzzle_Hall"), ui, new Vector2(4.3f, -2.4f), new Vector2(0.9f, 4.2f));

        // пані Стефанія (x = 11): розмова, лист, шляхи А / Б — у вузлі stefania в Ink
        GameObject stefania = Npc("NPC_Stefania", "Пані Стефанія", "Стара кравчиня. Не відводить очей від сукні.", "stefania", 11f,
                                  new Color(0.55f, 0.45f, 0.5f));
        var addressee = new SerializedObject(stefania.AddComponent<NPCAddressee>());
        addressee.FindProperty("addresseeId").stringValue = "stefaniia";   // як у Letter_Tailor
        addressee.FindProperty("deliveredKnot").stringValue = "stefania";
        addressee.FindProperty("closedFlag").stringValue = "tailor_closed";
        addressee.FindProperty("leftFlag").stringValue = "tailor_left";
        addressee.ApplyModifiedPropertiesWithoutUndo();

        // Стефанії не видно, коли вона зачинилась у задній кімнаті або пішла
        var visibility = new SerializedObject(new GameObject("Stefania_Visibility").AddComponent<FlagVisibility>());
        visibility.FindProperty("target").objectReferenceValue = stefania;
        SerializedProperty flags = visibility.FindProperty("hideFlags");
        flags.arraySize = 2;
        flags.GetArrayElementAtIndex(0).stringValue = "tailor_closed";
        flags.GetArrayElementAtIndex(1).stringValue = "tailor_left";
        visibility.ApplyModifiedPropertiesWithoutUndo();

        Examine("Examine_Mannequin", "Манекен із сукнею", "Весільна сукня. Майже готова — бракує одного ґудзика.",
                new Vector2(12.5f, -1.4f), new Vector2(0.9f, 2.2f));

        // двері задньої кімнати (x = 15): шлях Б — підсунути ґудзик
        Interactable door = SetInteractable(Area("Door_BackRoom", new Vector2(14.85f, 0.2f), new Vector2(1.5f, 5.3f)).AddComponent<Interactable>(),
                                            "Двері задньої кімнати", "Звичайні двері. Ручка відполірована до блиску.", InteractionType.Use,
                                            null, "Зачинено.", Item("item_button"));
        UnityEventTools.AddStringPersistentListener(door.onItemUsed, Dialogue().StartKnot, "stefania_button");

        // екрани «Главу 1 завершено» і «Межа пам'ятає» (задача 31)
        InstantiatePrefab(ChapterEndPrefab);

        EditorSceneManager.SaveScene(scene);
    }

    // ---------- Арка на Площі Пошти ----------
    private static void AddArchToPostSquare()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScene);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Door_TailorStreet") Object.DestroyImmediate(root);

        // арка на фоні background1 — x ≈ 9.8
        Door("Door_TailorStreet", "Арка на Вулицю Ниток", "Кована хвіртка в кам'яній арці.",
             new Vector2(9.8f, -0.2f), new Vector2(2.2f, 3.6f),
             "L1_TailorStreet", new Vector2(3f, -3.5f), "prologue_done", "Арка зачинена. Спершу — перший лист.");
        EditorSceneManager.SaveScene(scene);
    }

    // ---------- Допоміжне ----------

    // Копія Sandbox_Player без об'єктів Площі Пошти
    private static Scene CopySourceScene(string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(SourceScene, path);
        Scene scene = EditorSceneManager.OpenScene(path);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (PostSquareObjects.Contains(root.name)) Object.DestroyImmediate(root);
        return scene;
    }

    // Фон заввишки 9 од., лівий край — x = 0. Повертає ширину фону
    private static float Background(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        var go = new GameObject("Background");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;

        float scale = 9f / sprite.bounds.size.y;
        float width = sprite.bounds.size.x * scale;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.position = new Vector3(width / 2f, 0f, 0f);
        return width;
    }

    private static PolygonCollider2D WalkArea(string name, float x0, float x1, float top, float bottom)
    {
        var area = new GameObject(name).AddComponent<PolygonCollider2D>();
        area.isTrigger = true;
        area.points = new[] { new Vector2(x0, bottom), new Vector2(x1, bottom), new Vector2(x1, top), new Vector2(x0, top) };
        return area;
    }

    private static PlayerController SetupPlayerAndCamera(Collider2D walkArea, Vector2 start, float width)
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        player.transform.position = new Vector3(start.x, start.y, player.transform.position.z);
        var so = new SerializedObject(player);
        so.FindProperty("walkArea").objectReferenceValue = walkArea;
        so.ApplyModifiedPropertiesWithoutUndo();

        var cam = new SerializedObject(Object.FindAnyObjectByType<CameraFollow2D>());
        cam.FindProperty("minX").floatValue = 0f;
        cam.FindProperty("maxX").floatValue = width;
        cam.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    private static void FlagWalk(string flag, PlayerController player, Collider2D before, Collider2D after)
    {
        var so = new SerializedObject(new GameObject("FlagWalkArea").AddComponent<FlagWalkArea>());
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("flagName").stringValue = flag;
        so.FindProperty("areaBefore").objectReferenceValue = before;
        so.FindProperty("areaAfter").objectReferenceValue = after;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Кольоровий прямокутник із колайдером (заглушка, поки немає арту)
    private static GameObject Box(string name, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        sr.color = color;
        sr.sortingOrder = order;
        Vector2 s = sr.sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / s.x, size.y / s.y, 1f);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    // Невидимий прямокутник для кліку (двері, манекен — вони вже намальовані на фоні)
    private static GameObject Area(string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.AddComponent<BoxCollider2D>().size = size;
        return go;
    }

    private static Interactable SetInteractable(Interactable it, string displayName, string description, InteractionType type,
                                                ItemData item = null, string useText = "", ItemData requiredItem = null,
                                                string wrongItemText = "Так не вийде.")
    {
        var so = new SerializedObject(it);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("type").enumValueIndex = (int)type;
        so.FindProperty("item").objectReferenceValue = item;
        so.FindProperty("useText").stringValue = useText;
        so.FindProperty("requiredItem").objectReferenceValue = requiredItem;
        so.FindProperty("consumeItem").boolValue = false;
        so.FindProperty("useSuccessText").stringValue = "";
        so.FindProperty("wrongItemText").stringValue = wrongItemText;
        so.ApplyModifiedPropertiesWithoutUndo();
        return it;
    }

    private static GameObject Npc(string name, string displayName, string description, string knot, float x, Color color)
    {
        GameObject go = Box(name, new Vector2(x, NpcY), new Vector2(0.8f, 2.2f), color, 1);
        SetInteractable(go.AddComponent<Interactable>(), displayName, description, InteractionType.Talk);
        var dialogue = new SerializedObject(go.AddComponent<NPCDialogue>());
        dialogue.FindProperty("knot").stringValue = knot;
        dialogue.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void Examine(string name, string displayName, string description, Vector2 pos, Vector2 size)
    {
        SetInteractable(Area(name, pos, size).AddComponent<Interactable>(), displayName, description, InteractionType.Examine);
    }

    private static void Pickup(string name, string displayName, ItemData item, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = Box(name, pos, size, color, 2);
        SetInteractable(go.AddComponent<Interactable>(), displayName, item != null ? item.description : "", InteractionType.Pickup, item);
    }

    private static GameObject Door(string name, string displayName, string description, Vector2 pos, Vector2 size,
                                   string targetScene, Vector2 spawn, string requiredFlag, string lockedText)
    {
        GameObject go = Area(name, pos, size);
        SetInteractable(go.AddComponent<Interactable>(), displayName, description, InteractionType.Use);
        var door = new SerializedObject(go.AddComponent<SceneDoor>());
        door.FindProperty("targetScene").stringValue = targetScene;
        door.FindProperty("spawnPoint").vector2Value = spawn;
        door.FindProperty("requiredFlag").stringValue = requiredFlag;
        door.FindProperty("lockedText").stringValue = lockedText;
        door.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // Нитки поперек проходу: клік — пазл; ножиці на нитках — Марка бурчить (вузол marka_scissors)
    private static void ThreadBlocker(string name, ThreadPuzzleData data, ThreadPuzzleUI ui, Vector2 pos, Vector2 size)
    {
        GameObject go = Box(name, pos, size, ThreadColor, 3);
        Interactable it = SetInteractable(go.AddComponent<Interactable>(), "Нитки: " + data.Title,
                                          "Прохід обплутаний нитками. Їх можна розплутати.", InteractionType.Use,
                                          null, "Нитки натягнуті туго.", Item("item_scissors"));
        UnityEventTools.AddStringPersistentListener(it.onItemUsed, Dialogue().StartKnot, "marka_scissors");

        var trigger = new SerializedObject(go.AddComponent<ThreadPuzzleTrigger>());
        trigger.FindProperty("data").objectReferenceValue = data;
        trigger.FindProperty("puzzleUI").objectReferenceValue = ui;
        trigger.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject InstantiatePrefab(string path) =>
        (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));

    private static DialogueManager Dialogue() => Object.FindAnyObjectByType<DialogueManager>(FindObjectsInactive.Include);
    private static ItemData Item(string id) => AssetDatabase.LoadAssetAtPath<ItemData>(ItemsDir + id + ".asset");
    private static ThreadPuzzleData Puzzle(string name) => AssetDatabase.LoadAssetAtPath<ThreadPuzzleData>(PuzzlesDir + name + ".asset");

    private static void AddToBuildSettings(params string[] paths)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string path in paths)
            if (!scenes.Exists(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
