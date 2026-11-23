#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Одним пунктом меню ставить арт замість сірих квадратів:
//  1. Іконки сумки (Art/UI/Icons, 128×128) → поле icon у ItemData.
//  2. Чайник на Площі Пошти (Use_Kettle у Sandbox_Player) → obj_steam_kettle.
//  3. Вікно пари в усіх сценах, де воно є: чайник згори (kettle), анімована пара (kettle_steam + SteamWave),
//     конверт — великий аркуш, посередині якого печатка (її й водимо за парою).
//  4. Пазли ниток (ThreadPuzzleUI у сценах і префабі): плитка-папір, нитки, голка, котушки.
// Картинки взято з Art/Items/Cropped — копії без прозорих полів навколо (оригінали в Art/Items не змінені).
// Меню: LastLetter → Арт: іконки, чайник, пара, нитки. Можна запускати повторно.
public static class ArtKettleIconsSetup
{
    private const string Root = "Assets/_Project/";
    private const string Icons = Root + "Art/UI/Icons/";
    private const string Cropped = Root + "Art/Items/Cropped/";
    private const string Items = Root + "ScriptableObjects/Items/";
    private const string SteamMaterialPath = Root + "Art/Materials/Steam.mat";
    private const string ThreadPuzzlePrefab = Root + "Prefabs/UI/UI_ThreadPuzzle.prefab";

    // предмет → іконка
    private static readonly string[,] ItemIcons =
    {
        { "item_candle",     "icon_candle.png" },
        { "item_candle_lit", "icon_candle_lit.png" },
        { "item_matches",    "icon_matches.png" },
        { "item_red_thread", "icon_red_thread.png" },   // котушка червоної нитки (thread_spool_red)
        { "item_test_cube",  "icon_cube.png" },
    };

    private static readonly string[] Scenes =
    {
        Root + "Scenes/Sandbox/Sandbox_Player.unity",
        Root + "Scenes/L1_TailorStreet.unity",
        Root + "Scenes/L1_TailorHouse.unity",
        Root + "Scenes/Sandbox/Sandbox_Steam.unity",
        Root + "Scenes/Sandbox/Sandbox_Threads.unity",
    };

    // Розміри в одиницях (1 од. = 120 px). Змінив число — онови GDD (M2 / Огляд архітектури)
    private const float LevelKettleHeight = 2f;      // стіл із чайником у сцені
    private const float KettleWidth = 1.6f;          // чайник згори у вікні пари
    private const float SpoutY = -1.75f;             // де носик чайника = звідки йде пара
    private static readonly Vector2 SteamSize = new Vector2(1.4f, 6.5f);
    private const float SteamDrop = 0.3f;           // низ картинки пари прозорий — опускаємо її, щоб пара йшла просто з носика
    private static readonly Vector2 EnvelopeSize = new Vector2(3f, 1.9f);
    private const float SealSize = 0.25f;
    private static readonly Color PaperColor = new Color(0.95f, 0.9f, 0.78f);

    [MenuItem("LastLetter/Арт: іконки, чайник, пара, нитки")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        SetItemIcons();

        Sprite levelKettle = ImportSprite(Cropped + "obj_steam_kettle_crop.png", 120f);
        Sprite topKettle = ImportSprite(Cropped + "kettle_crop.png", 120f);
        Sprite steam = ImportSprite(Cropped + "kettle_steam_crop.png", 120f);
        if (levelKettle == null || topKettle == null || steam == null)
        {
            Debug.LogError("Немає картинок у " + Cropped);
            return;
        }
        Material steamMaterial = GetSteamMaterial(steam);
        ThreadSprites threads = LoadThreadSprites();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThreadPuzzlePrefab);
        if (prefab != null)
        {
            foreach (ThreadPuzzleUI ui in prefab.GetComponentsInChildren<ThreadPuzzleUI>(true)) SetThreadSprites(ui, threads);
            PrefabUtility.SavePrefabAsset(prefab);
        }

        foreach (string path in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path);
            bool changed = SetupLevelKettle(levelKettle);
            changed |= SetupSteamWindows(scene, topKettle, steam, steamMaterial);
            changed |= SetupThreadPuzzles(threads);
            if (!changed) continue;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Арт чайника і пари: " + path);
        }
        AssetDatabase.SaveAssets();
    }

    // ---------- 1. Іконки сумки ----------

    private static void SetItemIcons()
    {
        for (int i = 0; i < ItemIcons.GetLength(0); i++)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(Items + ItemIcons[i, 0] + ".asset");
            Sprite icon = ImportSprite(Icons + ItemIcons[i, 1], 128f);
            if (item == null || icon == null) { Debug.LogWarning("Пропущено іконку " + ItemIcons[i, 0]); continue; }
            item.icon = icon;
            EditorUtility.SetDirty(item);
        }
    }

    // ---------- 2. Чайник у сцені ----------

    // Сірий прямокутник Use_Kettle → стіл із чайником; низ лишається там, де був
    private static bool SetupLevelKettle(Sprite sprite)
    {
        GameObject kettle = GameObject.Find("Use_Kettle");
        if (kettle == null) return false;

        var sr = kettle.GetComponent<SpriteRenderer>();
        float bottom = sr.bounds.min.y;
        sr.sprite = sprite;
        sr.color = Color.white;

        float k = LevelKettleHeight / sprite.bounds.size.y;
        kettle.transform.localScale = new Vector3(k, k, 1f);
        Vector3 p = kettle.transform.position;
        kettle.transform.position = new Vector3(p.x, bottom + LevelKettleHeight * 0.5f, p.z);

        var col = kettle.GetComponent<BoxCollider2D>();
        if (col == null) col = kettle.AddComponent<BoxCollider2D>();
        col.offset = Vector2.zero;
        col.size = sprite.bounds.size;      // розмір у власних одиницях спрайта, масштаб застосує Unity
        return true;
    }

    // ---------- 3. Вікно пари ----------

    private static bool SetupSteamWindows(Scene scene, Sprite topKettle, Sprite steam, Material steamMaterial)
    {
        var games = Object.FindObjectsByType<SteamMinigame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SteamMinigame game in games)
        {
            var so = new SerializedObject(game);
            var column = so.FindProperty("steamColumn").objectReferenceValue as Transform;
            var envelope = so.FindProperty("envelope").objectReferenceValue as Transform;

            Transform kettle = game.transform.Find("Kettle");
            if (kettle == null) kettle = FindInScene(scene, "Kettle");   // у Sandbox_Steam усе лежить у корені сцени

            if (kettle != null) SetupTopKettle(kettle, topKettle);
            if (column != null) SetupSteam(column, steam, steamMaterial);
            if (envelope != null) SetupEnvelope(envelope);
        }
        return games.Length > 0;
    }

    // Чайник згори: носик дивиться вгору і стоїть рівно під парою
    private static void SetupTopKettle(Transform kettle, Sprite sprite)
    {
        var sr = kettle.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        float k = KettleWidth / sprite.bounds.size.x;
        kettle.localScale = new Vector3(k, k, 1f);
        kettle.localPosition = new Vector3(0f, SpoutY - sprite.bounds.size.y * k * 0.5f, kettle.localPosition.z);
    }

    // Колона пари: замість напівпрозорого прямокутника — дочірній об'єкт SteamVisual зі SteamWave.
    // Саму колону (її x) і далі рухає SteamMinigame, тож гра працює як раніше
    private static void SetupSteam(Transform column, Sprite steam, Material material)
    {
        int order = 102;
        var oldSr = column.GetComponent<SpriteRenderer>();
        bool firstRun = oldSr != null;      // після DestroyImmediate oldSr уже «== null», тож запам'ятовуємо
        if (firstRun)
        {
            order = oldSr.sortingOrder;
            Object.DestroyImmediate(oldSr);
        }
        column.localScale = Vector3.one;
        column.localPosition = new Vector3(column.localPosition.x, SpoutY, column.localPosition.z);

        Transform visual = column.Find("SteamVisual");
        if (visual == null)
        {
            visual = new GameObject("SteamVisual", typeof(MeshFilter), typeof(MeshRenderer), typeof(SteamWave)).transform;
            visual.SetParent(column, false);
        }
        visual.localPosition = new Vector3(0f, -SteamDrop, 0f);
        visual.GetComponent<MeshRenderer>().sharedMaterial = material;

        var wave = new SerializedObject(visual.GetComponent<SteamWave>());
        wave.FindProperty("sprite").objectReferenceValue = steam;
        wave.FindProperty("size").vector2Value = SteamSize;
        if (firstRun) wave.FindProperty("sortingOrder").intValue = order;   // повторний запуск — порядок уже записано
        wave.ApplyModifiedPropertiesWithoutUndo();
    }

    // Конверт → великий аркуш; печатка посередині лишається маленьким квадратиком
    private static void SetupEnvelope(Transform envelope)
    {
        var sr = envelope.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = PaperColor;
        envelope.localScale = new Vector3(EnvelopeSize.x, EnvelopeSize.y, 1f);

        foreach (string sealName in new[] { "SealIntact", "SealBroken" })
        {
            Transform seal = envelope.Find(sealName);
            if (seal == null) continue;
            seal.localPosition = new Vector3(0f, 0f, seal.localPosition.z);
            seal.localScale = new Vector3(SealSize / EnvelopeSize.x, SealSize / EnvelopeSize.y, 1f);   // аркуш розтягнутий — ділимо
        }
    }

    // ---------- 4. Пазли ниток ----------

    private class ThreadSprites
    {
        public Sprite tile, straight, corner, needle, spoolRed, spoolWhite;
    }

    private static ThreadSprites LoadThreadSprites()
    {
        return new ThreadSprites
        {
            tile = ImportSprite(Cropped + "thread_tile_base_crop.png", 120f),
            straight = ImportSprite(Cropped + "thread_straight_crop.png", 120f),   // біла нитка: колір дає ThreadPuzzleUI
            corner = ImportSprite(Cropped + "thread_corner_crop.png", 120f),
            needle = ImportSprite(Cropped + "thread_needle_crop.png", 120f),
            spoolRed = ImportSprite(Cropped + "thread_spool_red_crop.png", 120f),
            spoolWhite = ImportSprite(Cropped + "thread_spool_white_crop.png", 120f),
        };
    }

    private static bool SetupThreadPuzzles(ThreadSprites sprites)
    {
        var uis = Object.FindObjectsByType<ThreadPuzzleUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ThreadPuzzleUI ui in uis) SetThreadSprites(ui, sprites);
        return uis.Length > 0;
    }

    private static void SetThreadSprites(ThreadPuzzleUI ui, ThreadSprites s)
    {
        var so = new SerializedObject(ui);
        so.FindProperty("tileSprite").objectReferenceValue = s.tile;
        so.FindProperty("straightSprite").objectReferenceValue = s.straight;
        so.FindProperty("cornerSprite").objectReferenceValue = s.corner;
        so.FindProperty("needleSprite").objectReferenceValue = s.needle;
        so.FindProperty("spoolRedSprite").objectReferenceValue = s.spoolRed;
        so.FindProperty("spoolWhiteSprite").objectReferenceValue = s.spoolWhite;
        so.FindProperty("tileColor").colorValue = Color.white;   // плитка — малюнок, не фарбуємо
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Допоміжне ----------

    // Матеріал пари: шейдер для спрайтів без освітлення + текстура пари
    private static Material GetSteamMaterial(Sprite steam)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(SteamMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SteamMaterialPath));
            AssetDatabase.CreateAsset(material, SteamMaterialPath);
        }
        material.mainTexture = steam.texture;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    // Імпорт як один спрайт (Single) без мip-мап; пара ще й Clamp, щоб краї не «перетікали»
    private static Sprite ImportSprite(string path, float pixelsPerUnit)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = pixelsPerUnit;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
