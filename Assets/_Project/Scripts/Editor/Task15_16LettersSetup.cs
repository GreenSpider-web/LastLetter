#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Задачі 15–16 + звук: меню LastLetter → Task 15-16: Letters, kettle and sounds.
// Спершу мають бути виконані Task 12, 13, 14, 21, 22. Можна запускати повторно.
// Що робить:
//  1. Листи стають предметами сумки (item_letter_fisher, item_letter_tailor), у LetterData — описові адреси з Ink.
//  2. Префаб LetterManager — у MainMenu і Sandbox_Player.
//  3. Звуки: музика меню (і на паузі), сумка, кроки, теги #sfx в Ink.
//  4. Sandbox_Player: Поштмейстер видає лист 1 (якщо його ще немає — сірий), Пантелеймон приймає лист через Ink,
//     ліхтар зі спрайтами Софії, чайник із вікном пари.
//  5. Sandbox_Steam: SteamReader читає лист рибалці.
public static class Task15_16LettersSetup
{
    private const string PlayerScene = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string SteamScene = "Assets/_Project/Scenes/Sandbox/Sandbox_Steam.unity";
    private const string MenuScene = "Assets/_Project/Scenes/MainMenu.unity";

    private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items";
    private const string LettersDir = "Assets/_Project/ScriptableObjects/Letters";
    private const string SfxDir = "Assets/_Project/Audio/SFX";
    private const string SteamDataPath = LettersDir + "/SteamMinigameData.asset";

    private const string LetterManagerPrefab = "Assets/_Project/Prefabs/Managers/LetterManager.prefab";
    private const string InventoryPrefab = "Assets/_Project/Prefabs/Managers/InventoryManager.prefab";
    private const string DialoguePrefab = "Assets/_Project/Prefabs/UI/UI_Dialogue.prefab";
    private const string PausePrefab = "Assets/_Project/Prefabs/UI/UI_PauseMenu.prefab";
    private const string BagPrefab = "Assets/_Project/Prefabs/UI/UI_Bag.prefab";
    private const string PlayerPrefab = "Assets/_Project/Prefabs/Characters/Player.prefab";

    private const string MenuMusicPath = "Assets/_Project/Audio/Music/mus_main_theme.mp3";
    private const string FootstepPath = SfxDir + "/sfx_footsteps_cobble.ogg";
    private const string BagSoundPath = SfxDir + "/sfx_paper_bag.ogg";
    private const string LetterSoundPath = SfxDir + "/sfx_paper_letter.ogg";
    private const string SteamSoundPath = SfxDir + "/sfx_steam.mp3";

    private const string LanternOffPath = "Assets/_Project/Art/Items/obj_bridge_lantern_off.PNG";
    private const string LanternOnPath = "Assets/_Project/Art/Items/obj_bridge_lantern_on.PNG";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";

    [MenuItem("LastLetter/Task 15-16: Letters, kettle and sounds")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefab) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefab) == null)
        {
            Debug.LogError("Спершу запусти Task 14 і Task 22 з меню LastLetter.");
            return;
        }

        // 1. Листи — предмети сумки
        LetterData fisher = AssetDatabase.LoadAssetAtPath<LetterData>(LettersDir + "/Letter_Fisher.asset");
        LetterData tailor = AssetDatabase.LoadAssetAtPath<LetterData>(LettersDir + "/Letter_Tailor.asset");
        if (fisher == null || tailor == null) { Debug.LogError("Немає Letter_Fisher або Letter_Tailor у " + LettersDir); return; }

        ItemData fisherItem = GetOrCreateItem("item_letter_fisher", "Лист рибалці",
            "«Тому, хто ловить там, де немає води». Конверт запечатано.");
        ItemData tailorItem = GetOrCreateItem("item_letter_tailor", "Лист кравчині",
            "«Тій, що шиє для тієї, хто не прийде». Конверт запечатано.");
        SetupLetter(fisher, fisherItem, "Тому, хто ловить там, де немає води");
        SetupLetter(tailor, tailorItem, "Тій, що шиє для тієї, хто не прийде");

        // 2. Префаби менеджерів і UI
        SetupLetterManagerPrefab(fisher, tailor);
        EditPrefab<InventoryManager>(InventoryPrefab, so => FillAssets<ItemData>(so.FindProperty("allItems"), "t:ItemData", ItemsDir));
        EditPrefab<DialogueManager>(DialoguePrefab, so =>
        {
            FillAssets<ItemData>(so.FindProperty("items"), "t:ItemData", ItemsDir);
            FillAssets<AudioClip>(so.FindProperty("sounds"), "t:AudioClip", SfxDir);
        });
        EditPrefab<PauseMenu>(PausePrefab, so => so.FindProperty("pauseMusic").objectReferenceValue = Load<AudioClip>(MenuMusicPath));
        EditPrefab<UI_Bag>(BagPrefab, so => so.FindProperty("openSound").objectReferenceValue = Load<AudioClip>(BagSoundPath));
        EditPrefab<PlayerController>(PlayerPrefab, so => so.FindProperty("footstepSound").objectReferenceValue = Load<AudioClip>(FootstepPath));

        // 3. Головне меню: LetterManager і музика
        var menu = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        EnsureInstance<LetterManager>(LetterManagerPrefab);
        SetSceneMusic(Load<AudioClip>(MenuMusicPath));
        EditorSceneManager.SaveScene(menu);

        // 4. Sandbox_Steam: який лист читаємо
        var steam = EditorSceneManager.OpenScene(SteamScene, OpenSceneMode.Single);
        var steamReader = Object.FindAnyObjectByType<SteamReader>();
        if (steamReader != null) SetField(steamReader, "letter", fisher);
        EditorSceneManager.SaveScene(steam);

        // 5. Sandbox_Player
        var scene = EditorSceneManager.OpenScene(PlayerScene, OpenSceneMode.Single);
        if (GameObject.Find("NPC_Panteleimon") == null || GameObject.Find("Use_Lamp") == null)
        {
            Debug.LogError("У Sandbox_Player немає NPC_Panteleimon або Use_Lamp — спершу Task 12 і Task 14.");
            return;
        }
        EnsureInstance<LetterManager>(LetterManagerPrefab);
        SetSceneMusic(null);                // музика меню в грі не грає
        BuildPostmaster();
        SetupPanteleimon();
        SetupLantern();
        BuildKettle(fisher);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Задачі 15–16: готово. Play у MainMenu → Нова гра → Поштмейстер дає лист → (чайник) → ліхтар → Пантелеймон.");
    }

    // ---------- Листи ----------

    private static ItemData GetOrCreateItem(string id, string displayName, string description)
    {
        string path = ItemsDir + "/" + id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }
        item.id = id;
        item.displayName = displayName;
        item.description = description;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void SetupLetter(LetterData letter, ItemData item, string address)
    {
        var so = new SerializedObject(letter);
        so.FindProperty("item").objectReferenceValue = item;
        so.FindProperty("addressDescription").stringValue = address;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupLetterManagerPrefab(LetterData fisher, LetterData tailor)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LetterManagerPrefab) == null)
        {
            var go = new GameObject("LetterManager");
            go.AddComponent<LetterManager>();
            Directory.CreateDirectory(Path.GetDirectoryName(LetterManagerPrefab));
            PrefabUtility.SaveAsPrefabAsset(go, LetterManagerPrefab);
            Object.DestroyImmediate(go);
        }
        EditPrefab<LetterManager>(LetterManagerPrefab, so =>
        {
            var arr = so.FindProperty("letters");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = fisher;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = tailor;
        });
    }

    // ---------- Sandbox_Player ----------

    // Поштмейстер: вузол postmaster_intro видає лист 1 тегом #give_item.
    // Якщо Поштмейстер уже є (Артем: LastLetter → Add Postmaster intro, зі спрайтом і вступом) — не чіпаємо
    private static void BuildPostmaster()
    {
        if (GameObject.Find("NPC_Postmaster") != null) return;
        GameObject npc = MakeSquare("NPC_Postmaster", null, new Vector3(-5.5f, -1.9f, 0f),
                                    new Vector3(0.9f, 2.4f, 1f), new Color(0.55f, 0.45f, 0.4f), 0);
        npc.AddComponent<BoxCollider2D>();

        var it = npc.AddComponent<Interactable>();
        SetField(it, "displayName", "Поштмейстер");
        SetField(it, "description", "Високий чоловік у формі пошти. Дивиться крізь тебе.");
        SetEnum(it, "type", (int)InteractionType.Talk);

        SetField(npc.AddComponent<NPCDialogue>(), "knot", "postmaster_intro");
    }

    // Пантелеймон: розмова — panteleimon; лист 1 — panteleimon_letter, лише коли горить ліхтар
    private static void SetupPanteleimon()
    {
        GameObject npc = GameObject.Find("NPC_Panteleimon");
        var dialogue = npc.GetComponent<NPCDialogue>();
        if (dialogue == null) dialogue = npc.AddComponent<NPCDialogue>();
        SetField(dialogue, "knot", "panteleimon");

        var addressee = npc.GetComponent<NPCAddressee>();
        if (addressee == null) addressee = npc.AddComponent<NPCAddressee>();
        var so = new SerializedObject(addressee);
        so.FindProperty("addresseeId").stringValue = "pantelejmon";
        so.FindProperty("deliveredKnot").stringValue = "panteleimon_letter";
        so.FindProperty("wrongKnot").stringValue = "not_for_me";
        so.FindProperty("requiredFlag").stringValue = "lantern_lit";
        so.FindProperty("closedFlag").stringValue = "";
        so.FindProperty("leftFlag").stringValue = "";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Ліхтар: спрайти Софії (не горить / горить) за прапорцем lantern_lit
    private static void SetupLantern()
    {
        Sprite off = ImportSprite(LanternOffPath);
        Sprite on = ImportSprite(LanternOnPath);
        if (off == null || on == null) { Debug.LogWarning("Немає спрайтів ліхтаря — ліхтар лишається сірим."); return; }

        GameObject lamp = GameObject.Find("Use_Lamp");
        var sr = lamp.GetComponent<SpriteRenderer>();
        sr.sprite = off;
        sr.color = Color.white;

        // висота ліхтаря 2.6 од., низ стовпа на бруківці (y = -2.8)
        const float height = 2.6f;
        float k = height / off.bounds.size.y;
        lamp.transform.localScale = new Vector3(k, k, 1f);
        lamp.transform.position = new Vector3(lamp.transform.position.x, -2.8f + height * 0.5f, 0f);

        // клікати лише по стовпу, а не по прозорих краях картинки
        var col = lamp.GetComponent<BoxCollider2D>();
        if (col == null) col = lamp.AddComponent<BoxCollider2D>();
        col.offset = Vector2.zero;
        col.size = new Vector2(off.bounds.size.x * 0.35f, off.bounds.size.y);

        var flag = lamp.GetComponent<FlagSprite>();
        if (flag == null) flag = lamp.AddComponent<FlagSprite>();
        var so = new SerializedObject(flag);
        so.FindProperty("flagName").stringValue = "lantern_lit";
        so.FindProperty("spriteOff").objectReferenceValue = off;
        so.FindProperty("spriteOn").objectReferenceValue = on;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Чайник у сцені + вікно пари UI_Letter, прив'язане до камери
    private static void BuildKettle(LetterData defaultLetter)
    {
        Camera cam = Object.FindAnyObjectByType<Camera>();
        SteamMinigameData data = Load<SteamMinigameData>(SteamDataPath);
        if (cam == null || data == null) { Debug.LogError("Немає камери або SteamMinigameData — чайник не створено."); return; }

        // Вікно: дочірнє до камери, тож завжди в центрі екрана
        Replace("UI_Letter");
        var root = new GameObject("UI_Letter");
        root.transform.SetParent(cam.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, 10f);

        var window = new GameObject("Window");
        window.transform.SetParent(root.transform, false);
        MakeSquare("Dim", window.transform, Vector3.zero, new Vector3(40f, 20f, 1f), new Color(0f, 0f, 0f, 0.8f), 100);

        var game = new GameObject("SteamMinigame");
        game.transform.SetParent(window.transform, false);
        MakeSquare("Kettle", game.transform, new Vector3(0f, -3f, 0f), new Vector3(1.5f, 1f, 1f), new Color(0.45f, 0.45f, 0.45f), 101);
        float columnWidth = 2f * data.columnHalfWidthPx / data.pixelsPerUnit;
        GameObject column = MakeSquare("SteamColumn", game.transform, new Vector3(0f, -0.5f, 0f),
                                       new Vector3(columnWidth, 5f, 1f), new Color(0.85f, 0.9f, 1f, 0.55f), 102);
        GameObject envelope = MakeSquare("Envelope", game.transform, new Vector3(0f, 1.5f, 0f),
                                         new Vector3(0.7f, 0.45f, 1f), new Color(0.95f, 0.85f, 0.6f), 103);
        GameObject sealIntact = MakeSeal("SealIntact", envelope, new Color(0.75f, 0.15f, 0.15f));
        GameObject sealBroken = MakeSeal("SealBroken", envelope, new Color(0.25f, 0.25f, 0.25f));
        sealBroken.SetActive(false);

        var minigame = game.AddComponent<SteamMinigame>();
        var mso = new SerializedObject(minigame);
        mso.FindProperty("data").objectReferenceValue = data;
        mso.FindProperty("steamColumn").objectReferenceValue = column.transform;
        mso.FindProperty("envelope").objectReferenceValue = envelope.transform;
        mso.ApplyModifiedPropertiesWithoutUndo();

        var reader = game.AddComponent<SteamReader>();
        var rso = new SerializedObject(reader);
        rso.FindProperty("minigame").objectReferenceValue = minigame;
        rso.FindProperty("sealIntact").objectReferenceValue = sealIntact;
        rso.FindProperty("sealBroken").objectReferenceValue = sealBroken;
        rso.FindProperty("letter").objectReferenceValue = defaultLetter;
        rso.ApplyModifiedPropertiesWithoutUndo();

        var ui = root.AddComponent<UI_Letter>();
        var uso = new SerializedObject(ui);
        uso.FindProperty("window").objectReferenceValue = window;
        uso.FindProperty("minigame").objectReferenceValue = minigame;
        uso.FindProperty("reader").objectReferenceValue = reader;
        uso.FindProperty("steamLoop").objectReferenceValue = Load<AudioClip>(SteamSoundPath);
        uso.FindProperty("paperSound").objectReferenceValue = Load<AudioClip>(LetterSoundPath);
        uso.ApplyModifiedPropertiesWithoutUndo();
        window.SetActive(false);

        // Чайник у сцені (сірий прямокутник на бруківці)
        Replace("Use_Kettle");
        GameObject kettle = MakeSquare("Use_Kettle", null, new Vector3(-2f, -2.6f, 0f),
                                       new Vector3(1f, 0.8f, 1f), new Color(0.4f, 0.4f, 0.42f), 0);
        kettle.AddComponent<BoxCollider2D>();
        var it = kettle.AddComponent<Interactable>();
        SetField(it, "displayName", "Чайник");
        SetField(it, "description", "Старий чайник на жаровні. З носика йде пара.");
        SetEnum(it, "type", (int)InteractionType.Use);
        SetField(it, "useText", "Чайник тихо парує.");
        SetField(kettle.AddComponent<SteamKettle>(), "letterWindow", ui);
    }

    // ---------- Допоміжне ----------

    private static void SetSceneMusic(AudioClip clip)
    {
        var music = Object.FindAnyObjectByType<SceneMusic>();
        if (music == null) music = new GameObject("SceneMusic").AddComponent<SceneMusic>();
        SetField(music, "music", clip);
    }

    private static void EnsureInstance<T>(string prefabPath) where T : Component
    {
        if (Object.FindAnyObjectByType<T>() != null) return;
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
    }

    // Відкрити префаб, змінити поля компонента T і зберегти
    private static void EditPrefab<T>(string path, System.Action<SerializedObject> edit) where T : Component
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning("Немає префаба " + path); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        T comp = root.GetComponentInChildren<T>(true);
        if (comp == null) Debug.LogWarning($"У {path} немає {typeof(T).Name}");
        else
        {
            var so = new SerializedObject(comp);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void FillAssets<T>(SerializedProperty list, string filter, string folder) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
        list.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = Load<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
    }

    private static Sprite ImportSprite(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return Load<Sprite>(path);
    }

    private static GameObject MakeSquare(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Load<Sprite>(SquarePath);
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    // Печатка 0.18 × 0.18 од. посередині конверта (конверт розтягнутий, тож ділимо на його масштаб)
    private static GameObject MakeSeal(string name, GameObject envelope, Color color)
    {
        Vector3 s = envelope.transform.localScale;
        return MakeSquare(name, envelope.transform, Vector3.zero, new Vector3(0.18f / s.x, 0.18f / s.y, 1f), color, 104);
    }

    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    private static void SetField(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string field, int index)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).enumValueIndex = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Replace(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
    }
}
#endif
