#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Задача 13: меню LastLetter → Task 13: Add inventory to Sandbox_Player (спершу — Task 12).
// Створює асети предметів зрізу і рецепт, InventoryManager, вікно сумки UI_Bag
// і тестові предмети в сцені: Свічка, Сірники, Кубик; Ліхтар приймає Запалену свічку.
public static class Task13InventorySetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items/";
    private const string RecipesDir = "Assets/_Project/ScriptableObjects/Recipes/";
    private const string ManagerPrefab = "Assets/_Project/Prefabs/Managers/InventoryManager.prefab";
    private const string BagPrefab = "Assets/_Project/Prefabs/UI/UI_Bag.prefab";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";

    [MenuItem("LastLetter/Task 13: Add inventory to Sandbox_Player")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (GameObject.Find("Interaction") == null)
        {
            Debug.LogError("Спершу запусти LastLetter → Task 12: Add interaction to Sandbox_Player.");
            return;
        }

        // 1. Предмети зрізу (GDD → M4: 9 предметів, листи — це LetterData, задача 15)
        ItemData candle   = Item("item_candle",     "Свічка",                "Недогарок воскової свічки.");
        ItemData matches  = Item("item_matches",    "Сірники",               "Коробка сірників. Кілька ще сухі.");
        ItemData candleLit = Item("item_candle_lit", "Запалена свічка",      "Маленький вогник. Його треба берегти.");
        Item("item_scissors",   "Ножиці",                "Кравецькі ножиці.");
        Item("item_button",     "Ґудзик",                "Перламутровий ґудзик.");
        Item("item_red_thread", "Клубок червоної нитки", "Червона нитка, змотана в клубок.");
        Item("item_thimble",    "Наперсток",             "Старий латунний наперсток.");
        ItemData cube = Item("item_test_cube", "Кубик", "Тестовий предмет.");

        // 2. Рецепт: Свічка + Сірники → Запалена свічка
        Directory.CreateDirectory(RecipesDir);
        string recipePath = RecipesDir + "Recipe_CandleLit.asset";
        var recipe = AssetDatabase.LoadAssetAtPath<CombineRecipe>(recipePath);
        if (recipe == null)
        {
            recipe = ScriptableObject.CreateInstance<CombineRecipe>();
            AssetDatabase.CreateAsset(recipe, recipePath);
        }
        recipe.itemA = candle;
        recipe.itemB = matches;
        recipe.result = candleLit;
        recipe.successText = "Свічка горить.";
        EditorUtility.SetDirty(recipe);

        // 3. InventoryManager (префаб, щоб потім кинути в кожну сцену)
        Replace("InventoryManager");
        var managerObj = new GameObject("InventoryManager");
        var manager = managerObj.AddComponent<InventoryManager>();
        var mso = new SerializedObject(manager);
        var list = mso.FindProperty("recipes");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = recipe;
        mso.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(managerObj, ManagerPrefab);

        // 4. EventSystem — без нього кнопки UI не клікаються (новий Input System)
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // 5. Вікно сумки
        Replace("UI_Bag");
        GameObject bagObj = BuildBagUI();
        SavePrefab(bagObj, BagPrefab);

        // 6. Предмети в сцені
        SetItem("Item_Cube", cube);
        MakePickup("Item_Candle", "Свічка", "Недогарок свічки. Ще згодиться.", candle,
                   new Vector3(1f, -3.0f, 0f), new Vector3(0.25f, 0.6f, 1f), new Color(0.9f, 0.88f, 0.8f));
        MakePickup("Item_Matches", "Сірники", "Коробка сірників.", matches,
                   new Vector3(6.5f, -3.4f, 0f), new Vector3(0.5f, 0.3f, 1f), new Color(0.55f, 0.4f, 0.25f));

        GameObject lamp = GameObject.Find("Use_Lamp");
        if (lamp != null)
        {
            var lso = new SerializedObject(lamp.GetComponent<Interactable>());
            lso.FindProperty("requiredItem").objectReferenceValue = candleLit;
            lso.FindProperty("consumeItem").boolValue = true;
            lso.FindProperty("useSuccessText").stringValue = "Ліхтар засвітився.";
            lso.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Задача 13: сумку додано. Play → візьми Свічку і Сірники, Tab, клік по Свічці, потім по Сірниках.");
    }

    private static ItemData Item(string id, string displayName, string description)
    {
        Directory.CreateDirectory(ItemsDir);
        string path = ItemsDir + id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null) return item;      // уже є — не перезаписуємо (може, там уже іконка)

        item = ScriptableObject.CreateInstance<ItemData>();
        item.id = id;
        item.displayName = displayName;
        item.description = description;
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static void MakePickup(string objName, string displayName, string desc, ItemData item,
                                   Vector3 pos, Vector3 scale, Color color)
    {
        Replace(objName);
        var go = new GameObject(objName);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        sr.color = color;
        go.AddComponent<BoxCollider2D>();

        var so = new SerializedObject(go.AddComponent<Interactable>());
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = desc;
        so.FindProperty("type").enumValueIndex = (int)InteractionType.Pickup;
        so.FindProperty("item").objectReferenceValue = item;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetItem(string objName, ItemData item)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;
        var so = new SerializedObject(go.GetComponent<Interactable>());
        so.FindProperty("item").objectReferenceValue = item;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(go))
            PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
    }

    // Canvas → кнопка «Сумка» + панель 6×2 слотів
    private static GameObject BuildBagUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("UI_Bag", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        // кнопка «Сумка» в правому нижньому куті
        var btnObj = UIObject("BagButton", root.transform);
        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(1f, 0f);
        btnRect.anchoredPosition = new Vector2(-30f, 30f);
        btnRect.sizeDelta = new Vector2(200f, 64f);
        btnObj.AddComponent<Image>().color = new Color(0.3f, 0.25f, 0.2f, 0.95f);
        var bagButton = btnObj.AddComponent<Button>();
        Label(btnObj.transform, font, "Сумка (Tab)", 24);

        // панель зі слотами внизу по центру
        var panel = UIObject("Panel", root.transform);
        var pRect = panel.GetComponent<RectTransform>();
        pRect.anchorMin = pRect.anchorMax = pRect.pivot = new Vector2(0.5f, 0f);
        pRect.anchoredPosition = new Vector2(0f, 120f);
        pRect.sizeDelta = new Vector2(6 * 130 + 40, 2 * 130 + 40);
        panel.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.07f, 0.85f);
        var grid = panel.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120f, 120f);
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;

        var slots = new Button[12];
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = UIObject("Slot_" + i, panel.transform);
            slot.AddComponent<Image>();
            slots[i] = slot.AddComponent<Button>();

            var icon = UIObject("Icon", slot.transform);
            Stretch(icon.GetComponent<RectTransform>(), 10f);
            var iconImg = icon.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            Label(slot.transform, font, "", 18);
        }

        var bag = root.AddComponent<UI_Bag>();
        var so = new SerializedObject(bag);
        so.FindProperty("panel").objectReferenceValue = panel;
        var arr = so.FindProperty("slots");
        arr.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(bagButton.onClick, bag.Toggle);
        return root;
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Label(Transform parent, Font font, string text, int size)
    {
        var go = UIObject("Label", parent);
        Stretch(go.GetComponent<RectTransform>(), 6f);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.95f, 0.9f, 0.8f);
        t.raycastTarget = false;
    }

    private static void Stretch(RectTransform r, float pad)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(pad, pad);
        r.offsetMax = new Vector2(-pad, -pad);
    }

    private static void SavePrefab(GameObject go, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
    }

    private static void Replace(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
    }
}
#endif
