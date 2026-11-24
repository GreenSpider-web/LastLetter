#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Доповнює сцени L1_TailorStreet і L1_TailorHouse тим, що є на фонах, але ще не клікається, і туманом.
//  Вулиця Ниток: четверті чужі двері (x ≈ 35.6) і вікно, що світиться, над дверима кравчині (головна підказка рівня);
//                смуга туману над бруківкою, яка повільно гойдається (FogDrift).
//  Дім Кравчині: фотографії на стіні, швейна машинка, вікно із заходом сонця.
// Нічого з того, що вже є в сценах, не змінюється: об'єкт із такою назвою вже є — пропускається.
// Тексти оглядів — чернетка, Софія вичитує в задачі 38.
// Меню: LastLetter → Сцени глави 1: доповнити Вулицю Ниток і Дім Кравчині. Можна запускати повторно.
public static class TailorScenesPolish
{
    private const string StreetScene = "Assets/_Project/Scenes/L1_TailorStreet.unity";
    private const string HouseScene = "Assets/_Project/Scenes/L1_TailorHouse.unity";
    private const string FogPath = "Assets/_Project/Art/Backgrounds/fog_band.png";

    // Туман. Змінив число — онови GDD («Атмосфера» / «Візуальний контент»)
    private const float FogHeight = 4f;         // від низу кадру вгору, в одиницях
    private const float FogExtra = 2f;          // ширший за фон з обох боків, щоб при гойданні не було краю
    private const int FogOrder = 10;            // над NPC (1), предметами (2) і нитками (3), під вікнами пари й пазла (100+)

    [MenuItem("LastLetter/Сцени глави 1: доповнити Вулицю Ниток і Дім Кравчині")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // ---------- 1-Б Вулиця Ниток ----------
        Scene street = EditorSceneManager.OpenScene(StreetScene);
        Examine(street, "Examine_Door_36", "Чужі двері", "Зачинено. Тут ніхто не чекає листів.",
                new Vector2(35.6f, -1f), new Vector2(1f, 2.4f));
        Examine(street, "Examine_LitWindow", "Вікно, що світиться",
                "Світло горить, хоча сонце давно сіло. Ніби тут когось досі чекають.",
                new Vector2(41.65f, 2.3f), new Vector2(1f, 1.3f));
        AddFog(street);
        EditorSceneManager.MarkSceneDirty(street);
        EditorSceneManager.SaveScene(street);

        // ---------- 1-В Дім Кравчині ----------
        Scene house = EditorSceneManager.OpenScene(HouseScene);
        Examine(house, "Examine_Photos", "Фотографії",
                "Весільні й родинні знімки. На найбільшому одне обличчя затягнуте нитками.",
                new Vector2(8f, 1.2f), new Vector2(3.5f, 2.1f));
        Examine(house, "Examine_SewingMachine", "Швейна машинка",
                "Під голкою — мереживо. Робота зупинилась на пів стібка.",
                new Vector2(8.2f, -0.7f), new Vector2(3.6f, 1.6f));
        Examine(house, "Examine_Window", "Вікно",
                "За склом захід сонця. У Межі він ніколи не закінчується.",
                new Vector2(11.12f, 1.15f), new Vector2(1.75f, 2.2f));
        EditorSceneManager.MarkSceneDirty(house);
        EditorSceneManager.SaveScene(house);

        EditorSceneManager.OpenScene(StreetScene);
        Debug.Log("Вулицю Ниток і Дім Кравчині доповнено: нові об'єкти огляду і туман.");
    }

    // Невидима зона для ПКМ / ЛКМ — річ уже намальована на фоні
    private static void Examine(Scene scene, string name, string displayName, string description, Vector2 pos, Vector2 size)
    {
        if (Find(scene, name) != null) return;

        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = pos;
        go.AddComponent<BoxCollider2D>().size = size;

        var so = new SerializedObject(go.AddComponent<Interactable>());
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("type").enumValueIndex = (int)InteractionType.Examine;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Смуга туману на всю ширину фону, внизу кадру
    private static void AddFog(Scene scene)
    {
        if (Find(scene, "Fog_Front") != null) return;
        GameObject bg = Find(scene, "Background");
        if (bg == null || bg.GetComponent<SpriteRenderer>() == null) { Debug.LogWarning("Туман: немає Background"); return; }

        Sprite fog = ImportSprite(FogPath);
        if (fog == null) { Debug.LogWarning("Туман: немає " + FogPath); return; }

        Bounds b = bg.GetComponent<SpriteRenderer>().bounds;
        var go = new GameObject("Fog_Front");
        SceneManager.MoveGameObjectToScene(go, scene);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fog;
        sr.sortingOrder = FogOrder;

        float width = b.size.x + FogExtra * 2f;
        go.transform.localScale = new Vector3(width / fog.bounds.size.x, FogHeight / fog.bounds.size.y, 1f);
        go.transform.position = new Vector3(b.center.x, b.min.y + FogHeight * 0.5f, 0f);
        go.AddComponent<FogDrift>();
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    private static Sprite ImportSprite(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
