using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD (GDD → «Індикатори стану»):
//  1. Адреса — верхній лівий кут: «Адреса: …» або «Повернись на Пошту»;
//  2. Мапа — маленька кнопка-значок у верхньому правому куті або клавіша M; сумка — Tab;   // [CHANGED]
//  5. Сповіщення вгорі по центру — «Новий предмет: Свічка» (2 с); інші системи можуть викликати UI_HUD.Notify(...);
//  7. Значок автозбереження — штемпель, що крутиться, нижній правий кут, 1.5 с після кожного збереження.
// Підпис під курсором (4) малює InteractionText, світіння Марки (3) — HintSystem.
// HUD створюється сам у кожній сцені, де є Ліля (PlayerController), і ховається в головному меню.
public class UI_HUD : MonoBehaviour
{
    private const float ItemNoticeSeconds = 2f;
    private const float SaveIconSeconds = 1.5f;

    private static UI_HUD instance;

    private GameObject root;
    private Text addressText;
    private CanvasGroup notice;
    private Text noticeText;
    private GameObject saveIcon;
    private RectTransform stamp;
    private float noticeUntil;
    private float saveUntil;

    private readonly List<ItemData> knownItems = new List<ItemData>();   // що було в сумці — щоб знайти нове
    private bool subscribed;

    // Запускається сам після завантаження першої сцени гри
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => OnSceneLoaded();
        OnSceneLoaded();
    }

    private static void OnSceneLoaded()
    {
        bool gameScene = FindAnyObjectByType<PlayerController>() != null;
        if (instance == null)
        {
            if (!gameScene) return;
            instance = new GameObject("UI_HUD").AddComponent<UI_HUD>();
            DontDestroyOnLoad(instance.gameObject);
            instance.Build();
        }

        instance.root.SetActive(gameScene);
        if (gameScene) instance.Subscribe();
        instance.RefreshAddress();
    }

    // Показати сповіщення вгорі по центру (напр. «Марка згадав слово: …», 3 с)
    public static void Notify(string text, float seconds = ItemNoticeSeconds)
    {
        if (instance == null || !instance.root.activeSelf) return;
        instance.noticeText.text = text;
        instance.noticeUntil = Time.unscaledTime + seconds;
    }

    // Чи можна зараз відкрити вікно (мапу): немає паузи, розмови, листа, пазла
    public static bool CanOpenWindow()
    {
        if (PauseMenu.IsPaused || UI_LetterRead.IsOpen) return false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return false;
        ThreadPuzzleUI puzzle = FindAnyObjectByType<ThreadPuzzleUI>();
        return puzzle == null || !puzzle.IsOpen;
    }

    private void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChanged += OnInventoryChanged;
            knownItems.AddRange(InventoryManager.Instance.Items);
        }
        if (LetterManager.Instance != null) LetterManager.Instance.OnLetterDelivered += letter => RefreshAddress();
        SaveSystem.OnSaved += ShowSaveIcon;
    }

    private void Update()
    {
        if (!root.activeSelf) return;

        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) ToggleMap();

        // сповіщення: видно до noticeUntil, останні 0.3 с плавно зникає
        notice.alpha = Mathf.Clamp01((noticeUntil - Time.unscaledTime) / 0.3f);

        bool saving = Time.unscaledTime < saveUntil;
        if (saveIcon.activeSelf != saving) saveIcon.SetActive(saving);
        if (saving) stamp.Rotate(0f, 0f, -240f * Time.unscaledDeltaTime);
    }

    private void OnInventoryChanged()
    {
        IReadOnlyList<ItemData> items = InventoryManager.Instance.Items;

        // у головному меню («Продовжити») предмети з'являються мовчки
        if (root.activeSelf)
            foreach (ItemData item in items)
                if (!knownItems.Contains(item)) Notify("Новий предмет: " + item.displayName);

        knownItems.Clear();
        knownItems.AddRange(items);
        RefreshAddress();
    }

    // Адреса листа, який зараз у сумці і ще не доставлений
    private void RefreshAddress()
    {
        LetterData letter = CurrentLetter();
        addressText.text = letter != null ? "Адреса: " + letter.AddressDescription : "Повернись на Пошту";
    }

    private static LetterData CurrentLetter()
    {
        LetterManager letters = LetterManager.Instance;
        InventoryManager inv = InventoryManager.Instance;
        if (letters == null || inv == null) return null;

        foreach (LetterData letter in letters.Letters)
            if (letter != null && !letters.IsDelivered(letter) && inv.Has(letter.Item)) return letter;
        return null;
    }

    private void ShowSaveIcon() => saveUntil = Time.unscaledTime + SaveIconSeconds;

    // Кнопка сумки: те саме, що Tab (UI_Bag лежить у кожній сцені глави 1)   // [NEW]
    private void ToggleBag()                                                    // [NEW]
    {                                                                           // [NEW]
        if (UI_Map.IsOpen || !CanOpenWindow()) return;                          // [NEW]
        UI_Bag bag = FindAnyObjectByType<UI_Bag>();                             // [NEW]
        if (bag != null) bag.Toggle();                                          // [NEW]
    }                                                                           // [NEW]

    private void ToggleMap()
    {
        if (UI_Map.IsOpen) UI_Map.Close();
        else if (CanOpenWindow()) UI_Map.Open();
    }

    // ---------- Побудова HUD ----------

    private void Build()
    {
        UIBuild.MakeCanvas(gameObject, 5);   // під сумкою (10) і діалогом (20)
        root = UIBuild.Rect("HUD", transform).gameObject;
        UIBuild.Stretch((RectTransform)root.transform);

        // 1. Адреса — паперова етикетка
        Image label = UIBuild.Panel("Address", root.transform, new Vector2(0f, 1f), new Vector2(30f, -30f),
                                    new Vector2(700f, 70f), new Color(0.9f, 0.84f, 0.7f, 0.95f));
        Sprite labelSprite = Resources.Load<Sprite>("UI/ui_paper_label");                    // [NEW] Assets/_Project/Resources/UI/ui_paper_label.png
        if (labelSprite != null) { label.sprite = labelSprite; label.color = Color.white; }  // [NEW] паперова етикетка замість однотонної плашки
        addressText = UIBuild.Label("Text", label.transform, 28, FontStyle.Italic, TextAnchor.MiddleLeft, new Color(0.25f, 0.18f, 0.12f));
        addressText.rectTransform.offsetMin = new Vector2(20f, 0f);
        addressText.rectTransform.offsetMax = new Vector2(-20f, 0f);

        // 2. Кнопка мапи — значок у верхньому правому куті (кнопки «Сумка» і «Мапа» прибрали, сумка — Tab)   // [CHANGED]
        Button mapButton = UIBuild.MakeButton("Btn_Map", root.transform, Vector2.one, new Vector2(-30f, -30f), new Vector2(96f, 96f), "", ToggleMap);   // [NEW]
        Sprite mapIcon = Resources.Load<Sprite>("UI/icon_map");                                            // [NEW] Assets/_Project/Resources/UI/icon_map.png
        if (mapIcon != null) { Image img = mapButton.GetComponent<Image>(); img.sprite = mapIcon; img.color = Color.white; }   // [NEW]
        else mapButton.GetComponentInChildren<Text>().text = "M";                                           // [NEW] якщо значка нема — буква
        UIBuild.Label("Key", mapButton.transform, 22, FontStyle.Bold, TextAnchor.LowerRight, new Color(0.95f, 0.9f, 0.8f)).text = "M ";   // [NEW] підказка клавіші

        // 3. Кнопка сумки — значок ліворуч від мапи (те саме, що Tab)                                                   // [NEW]
        Button bagButton = UIBuild.MakeButton("Btn_Bag", root.transform, Vector2.one, new Vector2(-140f, -30f), new Vector2(96f, 96f), "", ToggleBag);   // [NEW]
        Sprite bagIcon = Resources.Load<Sprite>("UI/icon_bag");                                                         // [NEW] Assets/_Project/Resources/UI/icon_bag.png
        if (bagIcon != null) { Image img = bagButton.GetComponent<Image>(); img.sprite = bagIcon; img.color = Color.white; }   // [NEW]
        else bagButton.GetComponentInChildren<Text>().text = "Сумка";                                                   // [NEW] якщо значка нема — слово
        UIBuild.Label("Key", bagButton.transform, 22, FontStyle.Bold, TextAnchor.LowerRight, new Color(0.95f, 0.9f, 0.8f)).text = "Tab ";   // [NEW]

        // 5. Сповіщення
        Image noticeBg = UIBuild.Panel("Notice", root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                                       new Vector2(700f, 64f), new Color(0f, 0f, 0f, 0.6f));
        noticeBg.raycastTarget = false;
        notice = noticeBg.gameObject.AddComponent<CanvasGroup>();
        notice.alpha = 0f;
        notice.blocksRaycasts = false;
        noticeText = UIBuild.Label("Text", noticeBg.transform, 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f));

        // 7. Значок збереження: штемпель-кільце з вирізом (щоб було видно, що крутиться) і підпис
        Image icon = UIBuild.Panel("SaveIcon", root.transform, Vector2.right, new Vector2(-30f, 30f), new Vector2(260f, 80f), Color.clear);
        icon.raycastTarget = false;
        saveIcon = icon.gameObject;

        Text saved = UIBuild.Label("Text", icon.transform, 24, FontStyle.Italic, TextAnchor.MiddleLeft, new Color(0.95f, 0.9f, 0.8f));
        saved.text = "Збережено";

        Image ring = UIBuild.Panel("Stamp", icon.transform, Vector2.right, Vector2.zero, new Vector2(80f, 80f), new Color(0.75f, 0.2f, 0.15f));
        ring.sprite = UIBuild.DrawSprite(128, StampPixel);
        ring.raycastTarget = false;
        stamp = ring.rectTransform;
        stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.anchoredPosition = new Vector2(-40f, 40f);
        saveIcon.SetActive(false);
    }

    // Кільце штемпеля з вирізом угорі і крапкою в центрі
    private static Color StampPixel(float x, float y)
    {
        float r = Mathf.Sqrt(x * x + y * y);
        bool ring = r > 0.72f && r < 0.95f && !(y > 0.5f && Mathf.Abs(x) < 0.25f);
        bool dot = r < 0.22f;
        return ring || dot ? Color.white : Color.clear;
    }
}
