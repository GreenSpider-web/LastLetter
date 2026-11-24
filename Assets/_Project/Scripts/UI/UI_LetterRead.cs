using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Розгорнутий лист: після читання над парою з конверта розгортається біле полотно з текстом листа.
// Створюється сам при першому виклику UI_LetterRead.Show(...) — як SceneLoader, у сцену нічого класти не треба.
// Текст береться з LetterData (поле body), тож одне вікно підходить для всіх листів гри.
// Закрити — ПКМ. Після закриття викликається whenClosed (напр. репліка Марки).
// Полотно й шрифт — заглушка: потім замінимо на арт Софії.
public class UI_LetterRead : MonoBehaviour
{
    private const float UnfoldTime = 0.35f;     // за скільки секунд аркуш розгортається з конверта

    private static UI_LetterRead instance;

    private GameObject window;
    private RectTransform paper;
    private Text titleText;
    private Text bodyText;
    private PlayerController player;
    private Action onClosed;

    public static bool IsOpen => instance != null && instance.window.activeSelf;

    // Показати лист. whenClosed — що зробити, коли гравець його складе (можна null)
    public static void Show(LetterData letter, Action whenClosed = null)
    {
        if (letter == null) { Debug.LogWarning("UI_LetterRead: не передано лист"); return; }

        if (instance == null)
        {
            instance = new GameObject("UI_LetterRead").AddComponent<UI_LetterRead>();
            instance.Build();
        }
        instance.Open(letter, whenClosed);
    }

    private void Open(LetterData letter, Action whenClosed)
    {
        onClosed = whenClosed;
        titleText.text = letter.Title;
        bodyText.text = string.IsNullOrEmpty(letter.Body) ? "(Текст листа ще не написано.)" : letter.Body;

        player = FindAnyObjectByType<PlayerController>();
        if (player != null) { player.Stop(); player.CanMove = false; }

        window.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Unfold());
    }

    private void Update()
    {
        if (!window.activeSelf || PauseMenu.IsPaused) return;

        // ПКМ — скласти лист
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Close();
    }

    private void Close()
    {
        StopAllCoroutines();
        window.SetActive(false);
        StartCoroutine(EnableMoveNextFrame());

        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    // Аркуш виростає з вузької смужки (як із конверта) до повного розміру
    private IEnumerator Unfold()
    {
        for (float t = 0f; t < UnfoldTime; t += Time.unscaledDeltaTime)
        {
            float k = t / UnfoldTime;
            paper.localScale = new Vector3(Mathf.Lerp(0.3f, 1f, k), Mathf.Lerp(0.05f, 1f, k), 1f);
            yield return null;
        }
        paper.localScale = Vector3.one;
    }

    // Той самий ПКМ, що склав лист, не має одразу спрацювати в сцені як «оглянути»
    private IEnumerator EnableMoveNextFrame()
    {
        yield return null;
        bool talking = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;   // Марка вже говорить — рух увімкне діалог
        if (player != null && !talking) player.CanMove = true;
    }

    // ---------- Вікно: затемнення, біле полотно, назва, текст, підказка ----------

    private void Build()
    {
        // Canvas над діалогом (20), під паузою (50)
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // затемнення — ще й не пускає кліки в сцену
        window = NewRect("Window", transform).gameObject;
        Stretch((RectTransform)window.transform);
        window.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // аркуш старого паперу (якщо картинки нема — біле полотно)   // [CHANGED]
        paper = NewRect("Paper", window.transform);
        paper.sizeDelta = new Vector2(900f, 820f);
        Image paperImage = paper.gameObject.AddComponent<Image>();                                   // [CHANGED]
        paperImage.color = new Color(0.97f, 0.96f, 0.92f);                                           // [CHANGED]
        Sprite paperSprite = Resources.Load<Sprite>("UI/ui_letter_paper");                           // [NEW] Assets/_Project/Resources/UI/ui_letter_paper.png
        if (paperSprite != null) { paperImage.sprite = paperSprite; paperImage.color = Color.white; } // [NEW]

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // вбудований шрифт, кирилиця є
        var ink = new Color(0.2f, 0.15f, 0.1f);

        titleText = NewText("Title", paper, font, 44, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
        Place(titleText.rectTransform, new Vector2(80f, -130f), new Vector2(-80f, -40f));

        bodyText = NewText("Body", paper, font, 32, FontStyle.Italic, TextAnchor.UpperLeft, ink);
        bodyText.lineSpacing = 1.15f;
        Place(bodyText.rectTransform, new Vector2(80f, 110f), new Vector2(-80f, -150f));

        Text hint = NewText("Hint", paper, font, 24, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.45f, 0.4f, 0.35f));
        hint.text = "ПКМ — скласти лист";
        Place(hint.rectTransform, new Vector2(80f, 30f), new Vector2(-80f, -730f));

        window.SetActive(false);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Text NewText(string name, Transform parent, Font font, int size, FontStyle style, TextAnchor align, Color color)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    // Розтягнути по аркушу з відступами: offsetMin = (ліво, низ), offsetMax = (−право, −верх)
    private static void Place(RectTransform r, Vector2 offsetMin, Vector2 offsetMax)
    {
        Stretch(r);
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
    }
}
