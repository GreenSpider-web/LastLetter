#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// Збирає сцену Sandbox_Player для задачі 11: меню LastLetter → Create Sandbox_Player scene.
// Перед запуском виділи в Project малюнок персонажа — він стане Лілею. Нічого не виділено — буде сірий прямокутник.
// Після того як сцена і префаб збережені, цей скрипт більше не потрібен.
public static class SandboxPlayerSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string DataPath = "Assets/_Project/ScriptableObjects/Player/PlayerMovementData.asset";
    private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";
    private const string Bg1Path = "Assets/_Project/Art/Backgrounds/bg_mezha_no_death.png";
    private const string Bg2Path = "Assets/_Project/Art/Backgrounds/bg_death_mezha.png";
    private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";

    private const float PixelsPerUnit = 120f;   // за GDD: фон 1920×1080 = 16×9 одиниць
    private const float FloorBottom = -4.3f;    // низ бруківки
    private const float FloorTop = -1f;         // верх бруківки
    private const float LiliaHeight = 2.5f;     // зріст Лілі в одиницях (підбери під свій малюнок)

    [MenuItem("LastLetter/Create Sandbox_Player scene")]
    public static void Build()
    {
        // виділення читаємо до створення сцени
        Sprite character = GetSelectedSprite();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        PlayerMovementData data = GetOrCreateData();

        // Тимчасова «вулиця» з двох фонів: від x = -8 до x = 24
        MakeBackground("Background_1", Bg1Path, 0f, new Color(0.35f, 0.38f, 0.45f));
        MakeBackground("Background_2", Bg2Path, 16f, new Color(0.30f, 0.32f, 0.38f));

        // Підлога, по якій можна ходити
        var walkAreaObj = new GameObject("WalkArea");
        var walkArea = walkAreaObj.AddComponent<PolygonCollider2D>();
        walkArea.isTrigger = true;
        walkArea.points = new[]
        {
            new Vector2(-7.8f, FloorBottom), new Vector2(23.8f, FloorBottom),
            new Vector2(23.8f, FloorTop),    new Vector2(-7.8f, FloorTop)
        };

        // Ліля: Player (рух) → Visual (малюнок + анімація ходьби)
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, -3f, 0f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        if (character != null)
        {
            sr.sprite = character;
            float k = LiliaHeight / character.bounds.size.y;
            visual.transform.localScale = new Vector3(k, k, 1f);
            // якщо точка опори не під ногами (наприклад, по центру), піднімаємо малюнок, щоб ноги стояли на землі
            visual.transform.localPosition = new Vector3(0f, -character.bounds.min.y * k, 0f);
        }
        else
        {
            // сірий прямокутник з центром посередині -> піднімаємо, щоб низ був під ногами
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            sr.color = new Color(0.55f, 0.6f, 0.75f);
            visual.transform.localScale = new Vector3(0.8f, LiliaHeight, 1f);
            visual.transform.localPosition = new Vector3(0f, LiliaHeight * 0.5f, 0f);
        }

        var controller = player.AddComponent<PlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("data").objectReferenceValue = data;
        so.FindProperty("walkArea").objectReferenceValue = walkArea;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.ApplyModifiedPropertiesWithoutUndo();

        var bob = visual.AddComponent<WalkBob>();
        var bobSo = new SerializedObject(bob);
        bobSo.FindProperty("player").objectReferenceValue = controller;
        bobSo.ApplyModifiedPropertiesWithoutUndo();

        // Камера: 1080 px / 120 px на одиницю = 9 одиниць висоти, тому розмір 4.5
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        var follow = camObj.AddComponent<CameraFollow2D>();
        var camSo = new SerializedObject(follow);
        camSo.FindProperty("target").objectReferenceValue = player.transform;
        camSo.FindProperty("minX").floatValue = -8f;
        camSo.FindProperty("maxX").floatValue = 24f;
        camSo.ApplyModifiedPropertiesWithoutUndo();

        SetSortByY();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Сцену Sandbox_Player створено: " + ScenePath + ". Префаб: " + PrefabPath);
    }

    private static GameObject MakeSprite(string name, Sprite sprite, Vector3 pos, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        go.transform.position = pos;
        return go;
    }

    // Фон 16×9 одиниць. Якщо картинки фону ще немає в проєкті — сірий прямокутник того ж розміру
    private static void MakeBackground(string name, string path, float x, Color fallbackColor)
    {
        Sprite bg = LoadBackground(path);
        if (bg != null)
        {
            MakeSprite(name, bg, new Vector3(x, 0f, 0f), -10);
            return;
        }

        GameObject go = MakeSprite(name, AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath), new Vector3(x, 0f, 0f), -10);
        go.GetComponent<SpriteRenderer>().color = fallbackColor;
        go.transform.localScale = new Vector3(16f, 9f, 1f);

        // смуга бруківки, щоб було видно, де можна ходити
        GameObject floor = MakeSprite(name + "_Floor", AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath),
                                      new Vector3(x, (FloorBottom + FloorTop) * 0.5f, 0f), -9);
        floor.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.45f, 0.4f);
        floor.transform.localScale = new Vector3(16f, FloorTop - FloorBottom, 1f);
    }

    // Фон як спрайт зі 120 px на одиницю
    private static Sprite LoadBackground(string path)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti != null && (ti.textureType != TextureImporterType.Sprite || !Mathf.Approximately(ti.spritePixelsPerUnit, PixelsPerUnit)))
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Малюнок, виділений у Project: робимо його спрайтом з точкою опори внизу (під ногами)
    private static Sprite GetSelectedSprite()
    {
        Object sel = Selection.activeObject;
        if (sel == null) return null;

        string path = AssetDatabase.GetAssetPath(sel);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;

        // аркуш з кількома кадрами (Sprite Mode = Multiple): налаштування не чіпаємо, щоб не зламати нарізку кадрів
        if (ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Multiple)
        {
            if (sel is Sprite selected) return selected;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite first) return first;
            return null;
        }

        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        if (ti.textureType != TextureImporterType.Sprite || settings.spriteAlignment != (int)SpriteAlignment.BottomCenter)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
        return sel as Sprite ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static PlayerMovementData GetOrCreateData()
    {
        var data = AssetDatabase.LoadAssetAtPath<PlayerMovementData>(DataPath);
        if (data != null) return data;

        Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
        data = ScriptableObject.CreateInstance<PlayerMovementData>();
        data.nearY = FloorBottom;
        data.farY = FloorTop;
        AssetDatabase.CreateAsset(data, DataPath);
        return data;
    }

    // Хто нижче на екрані — той малюється попереду (Transparency Sort Mode = Custom Axis, вісь y)
    private static void SetSortByY()
    {
        Object renderer = AssetDatabase.LoadMainAssetAtPath(Renderer2DPath);
        if (renderer == null) return;

        var so = new SerializedObject(renderer);
        SerializedProperty mode = so.FindProperty("m_TransparencySortMode");
        SerializedProperty axis = so.FindProperty("m_TransparencySortAxis");
        if (mode == null || axis == null) return;

        mode.intValue = (int)TransparencySortMode.CustomAxis;
        axis.vector3Value = new Vector3(0f, 1f, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
