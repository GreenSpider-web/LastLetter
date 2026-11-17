#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Задача 12: меню LastLetter → Task 12: Add interaction to Sandbox_Player.
// Відкриває Sandbox_Player, додає Лілі PlayerInteractor, ставить курсори і три тестові об'єкти:
// кубик (взяти), ліхтар (використати), афіша (оглянути). Можна запускати повторно — старі тестові об'єкти замінюються.
public static class Task12InteractionSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string UiArt = "Assets/_Project/Art/UI/";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";
    private const string CubePrefabPath = "Assets/_Project/Prefabs/Interactables/Item_Cube.prefab";

    [MenuItem("LastLetter/Task 12: Add interaction to Sandbox_Player")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Texture2D examine = ImportCursor(UiArt + "cursor_examine.png");
        Texture2D take = ImportCursor(UiArt + "cursor_take.png");
        Texture2D use = ImportCursor(UiArt + "cursor_use.png");
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);

        // 1. Ліля: PlayerInteractor (і в префаб Player)
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("У сцені немає об'єкта Player (задача 11)."); return; }
        if (player.GetComponent<PlayerInteractor>() == null)
        {
            var comp = player.AddComponent<PlayerInteractor>();
            if (PrefabUtility.IsPartOfPrefabInstance(player))
                PrefabUtility.ApplyAddedComponent(comp, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player),
                                                  InteractionMode.AutomatedAction);
        }

        // 2. Курсори + тимчасовий текст
        Replace("Interaction");
        var interaction = new GameObject("Interaction");
        var cursors = interaction.AddComponent<CursorManager>();
        var cso = new SerializedObject(cursors);
        cso.FindProperty("examineCursor").objectReferenceValue = examine;
        cso.FindProperty("takeCursor").objectReferenceValue = take;
        cso.FindProperty("useCursor").objectReferenceValue = use;
        cso.ApplyModifiedPropertiesWithoutUndo();
        interaction.AddComponent<InteractionText>();

        // 3. Кубик — взяти (сірий квадрат, як у плані: етап 2 — «ядро на сірих квадратах»)
        Replace("Item_Cube");
        var cubeObj = new GameObject("Item_Cube");
        cubeObj.transform.position = new Vector3(4f, -2.8f, 0f);   // центр квадрата, низ на y = -3.2
        cubeObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);   // 0.8 × 0.8 од.
        var csr = cubeObj.AddComponent<SpriteRenderer>();
        csr.sprite = square;
        csr.color = new Color(0.6f, 0.6f, 0.6f);
        cubeObj.AddComponent<BoxCollider2D>();
        Setup(cubeObj.AddComponent<Interactable>(), "Кубик",
              "Дерев'яний кубик. Хтось загубив іграшку?", InteractionType.Pickup, "");
        Directory.CreateDirectory(Path.GetDirectoryName(CubePrefabPath));
        PrefabUtility.SaveAsPrefabAssetAndConnect(cubeObj, CubePrefabPath, InteractionMode.AutomatedAction);

        // 4. Ліхтар — використати
        Replace("Use_Lamp");
        var lamp = new GameObject("Use_Lamp");
        lamp.transform.position = new Vector3(9f, -1.6f, 0f);
        lamp.transform.localScale = new Vector3(0.3f, 2.4f, 1f);
        var lsr = lamp.AddComponent<SpriteRenderer>();
        lsr.sprite = square;
        lsr.color = new Color(0.25f, 0.22f, 0.2f);
        lamp.AddComponent<BoxCollider2D>();
        Setup(lamp.AddComponent<Interactable>(), "Ліхтар",
              "Старий газовий ліхтар. Скло закіпчене.", InteractionType.Use,
              "Ліхтар не запалюється. Потрібен вогонь.");

        // 5. Афіша — лише оглянути (на стіні, підходити не треба)
        Replace("Examine_Poster");
        var poster = new GameObject("Examine_Poster");
        poster.transform.position = new Vector3(-4f, 0.8f, 0f);
        poster.transform.localScale = new Vector3(1.2f, 1.6f, 1f);
        var psr = poster.AddComponent<SpriteRenderer>();
        psr.sprite = square;
        psr.color = new Color(0.85f, 0.78f, 0.62f);
        poster.AddComponent<BoxCollider2D>();
        Setup(poster.AddComponent<Interactable>(), "Афіша",
              "Вицвіла афіша. Дату концерту вже не прочитати.", InteractionType.Examine, "");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Задача 12: взаємодію додано в Sandbox_Player. Play → ПКМ по кубику — опис, ЛКМ — Ліля підходить і бере.");
    }

    private static void Setup(Interactable it, string name, string desc, InteractionType type, string useText)
    {
        var so = new SerializedObject(it);
        so.FindProperty("displayName").stringValue = name;
        so.FindProperty("description").stringValue = desc;
        so.FindProperty("type").enumValueIndex = (int)type;
        if (!string.IsNullOrEmpty(useText)) so.FindProperty("useText").stringValue = useText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Replace(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
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
}
#endif
