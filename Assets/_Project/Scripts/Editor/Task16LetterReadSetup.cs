#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Меню LastLetter → Task 16+: Letter icon + Postmaster intro.
// Необов'язкове: вікно з текстом листа (UI_LetterRead) створюється саме, а тексти вже лежать у LetterData.
// Працює з відкритою ігровою сценою (Sandbox_Player). Робить:
//  1. іконку-заглушку конверта для обох листів у сумці (якщо іконки ще немає);
//  2. Поштмейстера з IntroDialogue: на старті він сам заводить postmaster_intro і дає лист 1 у сумку.
// Меню можна запускати повторно. Потім збережи сцену (Ctrl+S).
public static class Task16LetterReadSetup
{
    private const string IconPath = "Assets/_Project/Art/Items/item_letter_placeholder.png";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";
    private const string PostmasterSpritePath = "Assets/_Project/Art/Characters/postmaster_idle.PNG";

    private static readonly string[] LetterItems =
    {
        "Assets/_Project/ScriptableObjects/Items/item_letter_fisher.asset",
        "Assets/_Project/ScriptableObjects/Items/item_letter_tailor.asset",
    };

    [MenuItem("LastLetter/Task 16+: Letter icon + Postmaster intro")]
    public static void Build()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Відкрий ігрову сцену з Лілею (Sandbox_Player), потім запусти меню.");
            return;
        }

        SetupLetterIcons();
        SetupPostmaster(player);

        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Debug.Log("Іконка листа і вступ Поштмейстера готові. Збережи сцену (Ctrl+S) і натисни Play.");
    }

    // ---------- 1. Іконка-заглушка: конверт із червоною печаткою, 128×128 ----------

    private static void SetupLetterIcons()
    {
        if (!File.Exists(IconPath))
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var paper = new Color(0.93f, 0.86f, 0.7f);
            var line = new Color(0.45f, 0.33f, 0.22f);
            var seal = new Color(0.7f, 0.12f, 0.12f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = x >= 8 && x < size - 8 && y >= 24 && y < size - 24;   // конверт 112×80
                Color c = inside ? paper : Color.clear;

                bool border = inside && (x < 11 || x >= size - 11 || y < 27 || y >= size - 27);
                // клапан: дві лінії від верхніх кутів до центру
                int flapY = size - 24 - (56 - Mathf.Abs(x - size / 2)) * 40 / 56;   // V: кути вгорі, вершина в центрі
                bool flap = inside && Mathf.Abs(y - flapY) <= 1;
                if (border || flap) c = line;

                float dx = x - size / 2f, dy = y - (size / 2f - 4f);
                if (dx * dx + dy * dy <= 13f * 13f) c = seal;          // печатка

                tex.SetPixel(x, y, c);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath);

            var ti = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 128;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        foreach (string path in LetterItems)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) { Debug.LogWarning("Немає " + path); continue; }
            if (item.icon != null) continue;     // справжню іконку (задача 30) не чіпаємо
            item.icon = icon;
            EditorUtility.SetDirty(item);
        }
        AssetDatabase.SaveAssets();
    }

    // ---------- 2. Поштмейстер: вступ і лист 1 ----------

    private static void SetupPostmaster(PlayerController player)
    {
        if (Object.FindAnyObjectByType<IntroDialogue>() != null) return;     // вступ уже є

        GameObject npc = GameObject.Find("NPC_Postmaster");

        // у сцені може стояти просто картинка Поштмейстера — робимо NPC з неї
        if (npc == null)
            foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (sr.sprite != null && sr.sprite.name.StartsWith("postmaster"))
                {
                    npc = sr.gameObject;
                    Undo.RecordObject(npc, "Rename Postmaster");
                    npc.name = "NPC_Postmaster";
                    break;
                }

        if (npc == null) npc = CreatePostmaster(player);

        if (npc.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(npc);

        Interactable it = npc.GetComponent<Interactable>();
        if (it == null)
        {
            it = Undo.AddComponent<Interactable>(npc);
            var iso = new SerializedObject(it);
            iso.FindProperty("displayName").stringValue = "Поштмейстер";
            iso.FindProperty("description").stringValue = "Худий чоловік у формі. Крізь круглі окуляри не видно очей.";
            iso.FindProperty("type").enumValueIndex = (int)InteractionType.Talk;
            iso.ApplyModifiedPropertiesWithoutUndo();
        }

        // повторний клік після вступу — коротка репліка (вступ із листом запускає IntroDialogue один раз)
        NPCDialogue talk = npc.GetComponent<NPCDialogue>();
        if (talk == null) talk = Undo.AddComponent<NPCDialogue>(npc);
        var dso = new SerializedObject(talk);
        dso.FindProperty("knot").stringValue = "postmaster_talk";
        dso.ApplyModifiedPropertiesWithoutUndo();

        Undo.AddComponent<IntroDialogue>(npc);
    }

    private static GameObject CreatePostmaster(PlayerController player)
    {
        var npc = new GameObject("NPC_Postmaster");
        Undo.RegisterCreatedObjectUndo(npc, "Add Postmaster");
        SceneManagerMove(npc, player.gameObject);

        var sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(PostmasterSpritePath);
        if (sr.sprite == null) { sr.sprite = LoadSprite(SquarePath); sr.color = new Color(0.35f, 0.3f, 0.3f); }

        float height = 2.8f;     // трохи вищий за Лілю (2.5 од.)
        float scale = height / Mathf.Max(0.01f, sr.sprite.bounds.size.y);
        npc.transform.localScale = new Vector3(scale, scale, 1f);

        // на тій самій лінії, що й Ліля, на 2.5 од. правіше (GDD: Поштмейстер x = 2.5)
        var playerSr = player.GetComponentInChildren<SpriteRenderer>();
        float feetY = playerSr != null ? playerSr.bounds.min.y : player.transform.position.y;
        npc.transform.position = new Vector3(player.transform.position.x + 2.5f, feetY, 0f);
        npc.transform.position += new Vector3(0f, feetY - sr.bounds.min.y, 0f);
        if (playerSr != null) { sr.sortingLayerID = playerSr.sortingLayerID; sr.sortingOrder = playerSr.sortingOrder; }
        return npc;
    }

    private static void SceneManagerMove(GameObject go, GameObject sameSceneAs)
    {
        if (go.scene != sameSceneAs.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, sameSceneAs.scene);
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

}
#endif
