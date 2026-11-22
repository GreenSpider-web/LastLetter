using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Підказки Марки (GDD → M6). H або ЛКМ по Марці.
// На кожну ціль глави 1 — 3 рівні: 1 — натяк, 2 — напрям, 3 — пряма вказівка.
// Тексти — у LastLetter_Ch1.ink, вузли hint_*: рівень N — це підвузол «.lN» (напр. hint_lantern.l2).
// На пазлі ниток рівень 3 ще й підсвічує плитку, повернуту не так.
// Після підказки — перезарядка 20 с (HintData); коли підказка готова, Марка світиться.
// Якщо підказка не готова або підказувати нічого — Марка просто каже «Я Марка, твій супутник!».
[RequireComponent(typeof(MarkaCompanion))]
public class HintSystem : MonoBehaviour
{
    [SerializeField] private HintData data;
    [Tooltip("Що Марка каже, коли підказка не готова")]
    [SerializeField] private string idleKnot = "marka_hello";

    private readonly Dictionary<string, int> levels = new Dictionary<string, int>();   // ціль → який рівень уже показано
    private float readyAt;          // з цього моменту (Time.time) можна наступну підказку
    private MarkaCompanion marka;

    // Чи Ліля вже говорила з Пантелеймоном (тоді знає адресата, і ціль — ліхтар). Живе між сценами
    private static bool metPanteleimon;

    private bool IsReady => Time.time >= readyAt;

    private void Awake()
    {
        marka = GetComponent<MarkaCompanion>();
        if (data == null) data = ScriptableObject.CreateInstance<HintData>();   // асет не вказано — числа за замовчуванням
        Interactable clickable = GetComponent<Interactable>();
        if (clickable != null) clickable.onInteract.AddListener(RequestHint);   // ЛКМ по Марці
    }

    private void Start()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
    }

    private void Update()
    {
        marka.SetGlow(IsReady && CurrentGoal() != null, data.glowColor, data.glowPeriod);

        if (PauseMenu.IsPaused || Keyboard.current == null) return;
        if (Keyboard.current.hKey.wasPressedThisFrame) RequestHint();
    }

    public void RequestHint()
    {
        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        string goal = CurrentGoal();
        if (goal == null || !IsReady)
        {
            dialogue.StartKnot(idleKnot);
            return;
        }

        // наступний рівень для цієї цілі: 1 → 2 → 3, далі лишається 3
        int level = levels.TryGetValue(goal, out int shown) ? Mathf.Min(shown + 1, 3) : 1;
        levels[goal] = level;
        readyAt = Time.time + data.hintCooldown;

        dialogue.StartKnot(goal + ".l" + level);

        ThreadPuzzleUI puzzle = OpenPuzzle();
        if (level == 3 && puzzle != null) puzzle.HighlightWrongTile();
    }

    // Яка зараз ціль (назва вузла hint_* в Ink); null — підказувати нічого
    private string CurrentGoal()
    {
        if (OpenPuzzle() != null)
            return Flag("puzzle_gate_solved") ? "hint_hall" : "hint_gate";

        if (Flag("tailor_closed") && !Flag("tailor_left"))
            return "hint_closed_door";

        if (Carries("fisher"))      // пролог: лист Пантелеймону
        {
            if (metPanteleimon && !Flag("lantern_lit")) return "hint_lantern";
            return "hint_fisher_address";
        }

        if (Carries("tailor"))      // глава 1: лист пані Стефанії
        {
            if (Flag("puzzle_gate_solved") && !Flag("puzzle_hall_solved")) return "hint_hall";
            return "hint_tailor_address";
        }

        return null;
    }

    private void OnDialogueEnded(string knot)
    {
        if (knot.StartsWith("panteleimon")) metPanteleimon = true;
    }

    private static ThreadPuzzleUI OpenPuzzle()
    {
        ThreadPuzzleUI puzzle = FindAnyObjectByType<ThreadPuzzleUI>();
        return puzzle != null && puzzle.IsOpen ? puzzle : null;
    }

    private static bool Flag(string name) => GameState.Instance != null && GameState.Instance.GetFlag(name);

    // Лист із таким id лежить у сумці і ще не доставлений
    private static bool Carries(string letterId)
    {
        LetterManager letters = LetterManager.Instance;
        InventoryManager inv = InventoryManager.Instance;
        if (letters == null || inv == null) return false;

        foreach (LetterData letter in letters.Letters)
            if (letter != null && letter.LetterId == letterId)
                return !letters.IsDelivered(letter) && inv.Has(letter.Item);
        return false;
    }
}
