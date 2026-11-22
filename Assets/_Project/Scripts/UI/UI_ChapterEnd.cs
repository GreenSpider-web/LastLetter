using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Кінець демо (задача 31, GDD → «Архітектура гри», екрани 10 і 11):
// коли пані Стефанія пішла (tailor_left) і розмова скінчилась —
// екран «Главу 1 завершено», потім «Межа пам'ятає» з підсумком проходження.
public class UI_ChapterEnd : MonoBehaviour
{
    [Header("Екран 10: Главу 1 завершено")]
    [SerializeField] private CanvasGroup chapterScreen;
    [SerializeField] private Button nextButton;

    [Header("Екран 11: Межа пам'ятає")]
    [SerializeField] private CanvasGroup demoEndScreen;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text markaText;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;

    [Header("Налаштування")]
    [SerializeField] private string menuScene = "MainMenu";
    [Tooltip("Репліка Марки на екрані «Межа пам'ятає» (як у вузлі demo_end_marka в Ink)")]
    [TextArea(1, 3)]
    [SerializeField] private string markaLine = "Далі буде важче. Там, у сортувальні, хтось стукає.";
    [Tooltip("Подарунки Стефанії: за ними видно, яким шляхом пройшов гравець")]
    [SerializeField] private ItemData giftPathA;     // Клубок червоної нитки
    [SerializeField] private ItemData giftPathB;     // Наперсток
    [Tooltip("Скільки секунд проявляється екран")]
    [SerializeField] private float fadeTime = 0.8f;

    private bool waiting;   // Стефанія пішла — чекаємо, поки закриється діалог
    private bool shown;

    // Усе, з чого складається підсумок (окремо — щоб перевірити всі варіанти без гри)
    public struct Result
    {
        public int delivered;
        public int totalLetters;
        public bool readFisher;
        public bool readTailor;
        public int intervention;
        public bool hasGiftA;
        public bool hasGiftB;
        public bool panteleimonLeft;
        public bool stefaniaLeft;
    }

    private void Start()
    {
        Hide(chapterScreen);
        Hide(demoEndScreen);

        nextButton.onClick.AddListener(ShowDemoEnd);
        menuButton.onClick.AddListener(() => SceneLoader.Load(menuScene));
        quitButton.onClick.AddListener(MainMenu.Quit);

        if (GameState.Instance != null) GameState.Instance.OnFlagChanged += OnFlagChanged;
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged -= OnFlagChanged;
    }

    private void OnFlagChanged(string flag, bool value)
    {
        if (flag == "tailor_left" && value && !shown) waiting = true;
    }

    private void Update()
    {
        if (!waiting) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return;   // спершу дослухати Стефанію
        waiting = false;
        ShowChapterEnd();
    }

    // ПКМ по компоненту в Inspector під час Play — перевірити екрани з поточним станом гри
    [ContextMenu("Тест: показати кінець глави")]
    public void ShowChapterEnd()
    {
        shown = true;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) { player.Stop(); player.CanMove = false; }
        StartCoroutine(FadeIn(chapterScreen));
    }

    private void ShowDemoEnd()
    {
        Hide(chapterScreen);
        summaryText.text = BuildSummary(Collect());
        markaText.text = "Марка: «" + markaLine + "»";
        StartCoroutine(FadeIn(demoEndScreen));
    }

    // Збираємо стан гри
    private Result Collect()
    {
        var r = new Result();
        GameState gs = GameState.Instance;
        if (gs != null)
        {
            r.readFisher = gs.GetFlag("read_letter_fisher");
            r.readTailor = gs.GetFlag("read_letter_tailor");
            r.intervention = gs.Intervention;
            r.panteleimonLeft = gs.GetFlag("prologue_done");
            r.stefaniaLeft = gs.GetFlag("tailor_left");
        }
        if (LetterManager.Instance != null)
        {
            r.delivered = LetterManager.Instance.DeliveredCount;
            r.totalLetters = LetterManager.Instance.Letters.Count;
        }
        if (InventoryManager.Instance != null)
        {
            r.hasGiftA = giftPathA != null && InventoryManager.Instance.Has(giftPathA);
            r.hasGiftB = giftPathB != null && InventoryManager.Instance.Has(giftPathB);
        }
        return r;
    }

    // Текст підсумку (GDD → «Ігровий процес → Завершення»)
    public static string BuildSummary(Result r)
    {
        var lines = new List<string>();

        lines.Add($"Листів доставлено: {r.delivered} з {r.totalLetters}");

        var read = new List<string>();
        if (r.readFisher) read.Add("лист рибалці");
        if (r.readTailor) read.Add("лист кравчині");
        lines.Add(read.Count == 0
            ? "Прочитано над парою: жодного"
            : $"Прочитано над парою: {read.Count} ({string.Join(", ", read)})");

        lines.Add($"Втручання: {r.intervention} з 2");

        if (r.hasGiftB)
        {
            lines.Add("Шлях у главі 1: Б — Стефанія дізналася, що лист читали, і зачинилася");
            lines.Add("Подарунок: Наперсток");
        }
        else if (r.hasGiftA)
        {
            lines.Add("Шлях у главі 1: А — лист віддано як є");
            lines.Add("Подарунок: Клубок червоної нитки");
        }

        var left = new List<string>();
        if (r.panteleimonLeft) left.Add("Пантелеймон");
        if (r.stefaniaLeft) left.Add("пані Стефанія");
        if (left.Count > 0) lines.Add("Перейшли міст: " + string.Join(", ", left));

        // GDD → M7: Втручання = 2 → «Нова Поштмейстерка», інакше → «Доставлено»
        string leaning = r.intervention >= 2 ? "«Нова Поштмейстерка»" : "«Доставлено»";
        lines.Add("");
        lines.Add("Межа схиляється до фіналу " + leaning);

        return string.Join("\n", lines);
    }

    private IEnumerator FadeIn(CanvasGroup screen)
    {
        screen.gameObject.SetActive(true);
        screen.blocksRaycasts = true;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            screen.alpha = t / fadeTime;
            yield return null;
        }
        screen.alpha = 1f;
    }

    private static void Hide(CanvasGroup screen)
    {
        screen.alpha = 0f;
        screen.blocksRaycasts = false;
        screen.gameObject.SetActive(false);
    }
}
