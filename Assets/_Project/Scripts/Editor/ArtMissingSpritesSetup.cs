#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ставить арт лише туди, де його ще НЕМАЄ: сірі квадрати (gray_square) і порожні поля.
// Те, що вже намальовано й вставлено, не чіпається.
//  1. Іконки сумки → ItemData, у яких icon порожній (ґудзик, наперсток, ножиці, обидва листи).
//  2. Сцени: NPC Стефанія, Жінка з парасолькою, Годинникар; ножиці й ґудзик на Вулиці Ниток;
//     нитки на хвіртці й у коридорі — замість сірих / червоних прямокутників.
//  3. Вікно пари: конверт із печаткою замість сірого аркуша, зламана печатка — конверт зі зламаною печаткою.
//  4. Портрети 512×512 для Стефанії, Жінки з парасолькою і Годинникаря (у діалогах, де портрет порожній).
// Картинки: Art/UI/Icons, Art/Items/Cropped (обрізані копії малюнків Софії), Art/ThreadTiles, Art/Characters/Portraits.
// Меню: LastLetter → Арт: поставити картинки там, де їх немає. Можна запускати повторно.
public static class ArtMissingSpritesSetup
{
    private const string Root = "Assets/_Project/";
    private const string Icons = Root + "Art/UI/Icons/";
    private const string Cropped = Root + "Art/Items/Cropped/";
    private const string Threads = Root + "Art/ThreadTiles/";
    private const string Portraits = Root + "Art/Characters/Portraits/";
    private const string Items = Root + "ScriptableObjects/Items/";
    private const string DialoguePrefab = Root + "Prefabs/UI/UI_Dialogue.prefab";

    // предмет → іконка 128×128
    private static readonly string[,] ItemIcons =
    {
        { "item_button",        "icon_button.png" },
        { "item_thimble",       "icon_thimble.png" },
        { "item_scissors",      "icon_scissors.png" },
        { "item_letter_fisher", "icon_letter_sealed.png" },
        { "item_letter_tailor", "icon_letter_sealed.png" },
    };

    // об'єкт у сцені → картинка і розмір в одиницях (1 од. = 120 px).
    // NPC — за висотою, ноги лишаються там, де стояв сірий прямокутник; дрібні предмети — за шириною.
    // Змінив число — онови GDD («Дизайн рівнів» / «Візуальний контент»)
    private static readonly SceneArt[] Figures =
    {
        new SceneArt("NPC_Stefania",      "npc_stefania_crop.png",       2.5f,  false),
        new SceneArt("NPC_UmbrellaWoman", "npc_umbrella_woman_crop.png", 2.85f, false),   // разом із парасолькою
        new SceneArt("NPC_Clockmaker",    "npc_clockmaker_crop.png",     2.35f, false),   // сутулий
        new SceneArt("Item_Scissors",     "scissors_crop.png",           0.8f,  true),
        new SceneArt("Item_Button",       "button_crop.png",             0.35f, true),
    };
    private const float MinClickSize = 0.6f;    // дрібний предмет усе одно клікається по квадрату 0.6×0.6

    // нитки-блокери: де висять (центр і розмір в одиницях, за фоном).
    // Хвіртка — на весь паркан (x 37–39.9) від верху паркану до бруківки;
    // коридор — завіса від стелі до підлоги перед проходом (x 3.75–4.95)
    private static readonly ThreadPlace[] ThreadArt =
    {
        new ThreadPlace("ThreadPuzzle_Gate", "thread_knot_gate.png", new Vector2(38.45f, -2.55f), new Vector2(2.9f, 3.5f)),
        new ThreadPlace("ThreadPuzzle_Hall", "thread_knot_hall.png", new Vector2(4.35f, -0.6f), new Vector2(1.2f, 7.6f)),
    };

    private class ThreadPlace
    {
        public readonly string objectName, file;
        public readonly Vector2 center, size;
        public ThreadPlace(string objectName, string file, Vector2 center, Vector2 size)
        {
            this.objectName = objectName; this.file = file; this.center = center; this.size = size;
        }
    }

    // id мовця (як у тегу #speaker) → портрет
    private static readonly string[,] SpeakerPortraits =
    {
        { "stefania",       "portrait_stefania_neutral.png" },
        { "umbrella_woman", "portrait_umbrella_woman_neutral.png" },
        { "clockmaker",     "portrait_clockmaker_neutral.png" },
    };

    private const float BrokenCover = 1.02f;    // зламаний конверт трохи більший, щоб повністю закрити цілий

    private class SceneArt
    {
        public readonly string objectName, file;
        public readonly float size;
        public readonly bool byWidth;
        public SceneArt(string objectName, string file, float size, bool byWidth)
        {
            this.objectName = objectName; this.file = file; this.size = size; this.byWidth = byWidth;
        }
    }

    [MenuItem("LastLetter/Арт: поставити картинки там, де їх немає")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // картинки для HUD і вікна листа беруться з Resources під час гри
        ImportSprite(Root + "Resources/UI/ui_letter_paper.png", 100f);
        ImportSprite(Root + "Resources/UI/ui_paper_label.png", 100f);
        ImportSprite(Root + "Resources/UI/icon_bag.png", 100f);

        SetItemIcons();
        SetPrefabPortraits();

        Sprite sealedLetter = ImportSprite(Cropped + "letter_sealed_crop.png", 120f);
        Sprite brokenLetter = ImportSprite(Cropped + "letter_broken_crop.png", 120f);

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { Root + "Scenes" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path);

            bool changed = SetFigures(scene);
            changed |= SetThreads(scene);
            changed |= SetEnvelopes(sealedLetter, brokenLetter);
            changed |= SetScenePortraits();
            if (!changed) continue;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Арт поставлено: " + path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Готово. Уже вставлені картинки не змінювались.");
    }

    // ---------- 1. Іконки сумки ----------

    private static void SetItemIcons()
    {
        for (int i = 0; i < ItemIcons.GetLength(0); i++)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(Items + ItemIcons[i, 0] + ".asset");
            if (item == null || item.icon != null) continue;     // іконка вже є — не чіпаємо

            Sprite icon = ImportSprite(Icons + ItemIcons[i, 1], 128f);
            if (icon == null) { Debug.LogWarning("Немає іконки " + ItemIcons[i, 1]); continue; }
            item.icon = icon;
            EditorUtility.SetDirty(item);
        }
    }

    // ---------- 2. NPC, предмети, нитки ----------

    private static bool SetFigures(Scene scene)
    {
        bool changed = false;
        foreach (SceneArt art in Figures)
        {
            GameObject go = FindInScene(scene, art.objectName);
            if (go == null) continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || !IsPlaceholder(sr)) continue;

            Sprite sprite = ImportSprite(Cropped + art.file, 120f);
            if (sprite == null) { Debug.LogWarning("Немає картинки " + art.file); continue; }

            float bottom = sr.bounds.min.y;
            sr.sprite = sprite;
            sr.color = Color.white;

            Vector2 s = sprite.bounds.size;
            float k = art.size / (art.byWidth ? s.x : s.y);
            go.transform.localScale = new Vector3(k, k, 1f);
            Vector3 p = go.transform.position;
            go.transform.position = new Vector3(p.x, bottom + s.y * k * 0.5f, p.z);   // низ лишається на місці

            var col = go.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                float min = MinClickSize / k;    // у власних одиницях спрайта, масштаб застосує Unity
                col.offset = Vector2.zero;
                col.size = new Vector2(Mathf.Max(s.x, min), Mathf.Max(s.y, min));
            }
            changed = true;
        }
        return changed;
    }

    private static bool SetThreads(Scene scene)
    {
        bool changed = false;
        foreach (ThreadPlace place in ThreadArt)
        {
            GameObject go = FindInScene(scene, place.objectName);
            if (go == null) continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || !IsPlaceholder(sr)) continue;

            Sprite sprite = ImportSprite(Threads + place.file, 120f);
            if (sprite == null) { Debug.LogWarning("Немає картинки " + place.file); continue; }

            sr.sprite = sprite;
            sr.color = Color.white;

            Vector2 s = sprite.bounds.size;
            go.transform.localScale = new Vector3(place.size.x / s.x, place.size.y / s.y, 1f);
            go.transform.position = new Vector3(place.center.x, place.center.y, go.transform.position.z);

            // клікати можна по всіх нитках (колайдер = картинка); ходити далі не пускає FlagWalkArea, як і раніше
            foreach (BoxCollider2D col in go.GetComponents<BoxCollider2D>())
            {
                col.offset = Vector2.zero;
                col.size = s;
            }
            changed = true;
        }
        return changed;
    }

    // ---------- 3. Конверт у вікні пари ----------

    // Конверт (Envelope) — малюнок цілого конверта тієї ж ширини.
    // SealIntact — печатка вже є на малюнку, тому її квадрат ховаємо.
    // SealBroken — увесь конверт зі зламаною печаткою поверх цілого (вмикає SteamReader, як і раніше)
    private static bool SetEnvelopes(Sprite sealedLetter, Sprite brokenLetter)
    {
        if (sealedLetter == null || brokenLetter == null) return false;
        bool changed = false;

        var readers = Object.FindObjectsByType<SteamReader>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SteamReader reader in readers)
        {
            var so = new SerializedObject(reader);
            var intact = so.FindProperty("sealIntact").objectReferenceValue as GameObject;
            var broken = so.FindProperty("sealBroken").objectReferenceValue as GameObject;
            if (intact == null || broken == null || intact.transform.parent == null) continue;

            Transform envelope = intact.transform.parent;
            var sr = envelope.GetComponent<SpriteRenderer>();
            if (sr == null || !IsPlaceholder(sr)) continue;     // конверт уже намальовано

            float width = sr.bounds.size.x;                     // ширину конверта не змінюємо
            float parentScale = envelope.parent != null ? envelope.parent.lossyScale.x : 1f;
            Vector2 a = sealedLetter.bounds.size;
            Vector2 b = brokenLetter.bounds.size;

            sr.sprite = sealedLetter;
            sr.color = Color.white;
            float k = width / (a.x * parentScale);
            envelope.localScale = new Vector3(k, k, 1f);

            var intactSr = intact.GetComponent<SpriteRenderer>();
            if (intactSr != null && IsPlaceholder(intactSr)) intactSr.enabled = false;

            var brokenSr = broken.GetComponent<SpriteRenderer>();
            if (brokenSr != null && IsPlaceholder(brokenSr))
            {
                brokenSr.sprite = brokenLetter;
                brokenSr.color = Color.white;
                float c = BrokenCover * a.x / b.x;
                broken.transform.localScale = new Vector3(c, c, 1f);
                broken.transform.localPosition = new Vector3(0f, (b.y * c - a.y) * 0.5f, broken.transform.localPosition.z);   // низи збігаються
            }
            changed = true;
        }
        return changed;
    }

    // ---------- 4. Портрети ----------

    private static void SetPrefabPortraits()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefab) == null) return;
        GameObject root = PrefabUtility.LoadPrefabContents(DialoguePrefab);
        bool changed = false;
        foreach (DialogueManager dm in root.GetComponentsInChildren<DialogueManager>(true)) changed |= SetPortraits(dm);
        if (changed) PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefab);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static bool SetScenePortraits()
    {
        bool changed = false;
        foreach (DialogueManager dm in Object.FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            changed |= SetPortraits(dm);
        return changed;
    }

    private static bool SetPortraits(DialogueManager dm)
    {
        var so = new SerializedObject(dm);
        SerializedProperty speakers = so.FindProperty("speakers");
        bool changed = false;
        for (int i = 0; i < speakers.arraySize; i++)
        {
            SerializedProperty speaker = speakers.GetArrayElementAtIndex(i);
            SerializedProperty portrait = speaker.FindPropertyRelative("portrait");
            if (portrait.objectReferenceValue != null) continue;    // портрет уже є — не чіпаємо

            string file = PortraitFile(speaker.FindPropertyRelative("id").stringValue);
            if (file == null) continue;
            Sprite sprite = ImportSprite(Portraits + file, 100f);
            if (sprite == null) continue;
            portrait.objectReferenceValue = sprite;
            changed = true;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return changed;
    }

    private static string PortraitFile(string id)
    {
        for (int i = 0; i < SpeakerPortraits.GetLength(0); i++)
            if (SpeakerPortraits[i, 0] == id) return SpeakerPortraits[i, 1];
        return null;
    }

    // ---------- Допоміжне ----------

    // Заглушка = порожньо або сірий квадрат
    private static bool IsPlaceholder(SpriteRenderer sr) =>
        sr.sprite == null || sr.sprite.texture == null || sr.sprite.texture.name == "gray_square";

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    // Імпорт як один спрайт (Single) без міп-мап. Лише для нових картинок цього скрипта
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
