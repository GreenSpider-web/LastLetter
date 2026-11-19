using System;
using UnityEngine;

// Стани адресата. Closed і Left потрібні для Стефанії (прапорці tailor_closed / tailor_left, GDD);
// [CHANGED] ставляться самі з прапорців GameState (поля closedFlag / leftFlag).
public enum NPCState
{
    Waiting,    // чекає на лист
    Received,   // лист уже отримав
    Closed,     // двері зачинені
    Left        // пішов
}

// Адресат листа: відповідає на вручення залежно від стану й від того, чи лист його.
// [CHANGED] Відповідає діалог Ink (deliveredKnot / wrongKnot); прості репліки нижче — коли Ink у сцені немає.
// Лист вручається як предмет із сумки: PlayerInteractor викликає TryReceive (IItemReceiver).
public class NPCAddressee : MonoBehaviour, IItemReceiver   // [CHANGED]
{
    [SerializeField] private string addresseeId = "pantelejmon";
    [SerializeField] private NPCState state = NPCState.Waiting;

    [Header("Ink")]   // [NEW]
    [Tooltip("Вузол Ink, коли Ліля віддає свій лист, напр. panteleimon_letter")]
    [SerializeField] private string deliveredKnot = "";
    [Tooltip("Вузол Ink, коли лист не цьому адресату")]
    [SerializeField] private string wrongKnot = "not_for_me";
    [Tooltip("Без цього прапорця лист не приймається (Пантелеймону потрібне світло — lantern_lit). Порожньо — приймає завжди")]
    [SerializeField] private string requiredFlag = "";

    [Header("Прапорці GameState → стан")]   // [NEW]
    [Tooltip("Прапорець «зачинився», напр. tailor_closed")]
    [SerializeField] private string closedFlag = "";
    [Tooltip("Прапорець «пішов», напр. tailor_left. Якщо вказано, лист забирають у діалозі Ink, а доставленим він стає разом із цим прапорцем")]
    [SerializeField] private string leftFlag = "";

    [Header("Репліки")]
    [SerializeField] private string wrongLetterReply = "Це не мені.";
    [SerializeField] private string noLetterReply = "Ви до мене?";
    [SerializeField] private string receivedReply = "Дякую, лист я вже отримав.";
    [SerializeField] private string deliveredReply = "Це мені! Дякую.";
    [SerializeField] private string closedReply = "Двері зачинені.";
    [SerializeField] private string leftReply = "Тут уже нікого немає.";

    public string AddresseeId => addresseeId;
    public NPCState State => state;

    public event Action<NPCState> OnStateChanged;
    public event Action<string> OnReply;   // для майбутнього UI

    // [NEW] Стан з прапорців: після діалогу Ink і після «Продовжити»
    private void Start()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged += OnFlagChanged;
        ApplyFlags();

        // лист цьому адресату доставили раніше (зі збереження)
        LetterManager manager = LetterManager.Instance;
        if (state == NPCState.Waiting && manager != null && manager.IsDelivered(manager.FindByAddressee(addresseeId)))
            SetState(NPCState.Received);
    }

    private void OnDestroy()   // [NEW]
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged -= OnFlagChanged;
    }

    private void OnFlagChanged(string flag, bool value)   // [NEW]
    {
        if (flag == closedFlag || flag == leftFlag) ApplyFlags();
    }

    // [NEW]
    private void ApplyFlags()
    {
        GameState gs = GameState.Instance;
        if (gs == null) return;

        if (!string.IsNullOrEmpty(leftFlag) && gs.GetFlag(leftFlag))
        {
            // забрав лист у діалозі й пішов (Стефанія: шлях А або ґудзик на шляху Б)
            LetterManager manager = LetterManager.Instance;
            if (manager != null) manager.MarkDelivered(manager.FindByAddressee(addresseeId));
            SetState(NPCState.Left);
        }
        else if (!string.IsNullOrEmpty(closedFlag) && gs.GetFlag(closedFlag)) SetState(NPCState.Closed);
        else if (state == NPCState.Closed) SetState(NPCState.Waiting);   // двері знову відчинились
    }

    // [NEW] Ліля використала предмет із сумки на адресаті
    public bool TryReceive(ItemData item)
    {
        LetterManager manager = LetterManager.Instance;
        LetterData letter = manager != null ? manager.FindByItem(item) : null;
        if (letter == null) return false;     // не лист — звичайне «Так не вийде»

        manager.SelectLetter(letter);
        Interact();

        // прибрати лист із «руки»: якщо його не віддали, він лишається в сумці
        manager.SelectLetter(null);
        if (InventoryManager.Instance != null) InventoryManager.Instance.Deselect();
        return true;
    }

    public void SetState(NPCState newState)
    {
        if (state == newState) return;
        state = newState;
        OnStateChanged?.Invoke(state);
    }

    // Гравець підійшов до NPC й вручає обраний лист. Повертає репліку NPC.
    public string Interact()
    {
        string reply = GetReply();
        if (reply == null) return null;       // [NEW] відповідає діалог Ink
        Debug.Log($"NPC {addresseeId}: {reply}");
        OnReply?.Invoke(reply);
        if (InteractionText.Instance != null) InteractionText.Instance.Show(reply);   // [NEW]
        return reply;
    }

    private string GetReply()
    {
        if (state == NPCState.Closed) return closedReply;
        if (state == NPCState.Left) return leftReply;
        if (state == NPCState.Received) return receivedReply;

        LetterManager manager = LetterManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("NPCAddressee: у сцені немає LetterManager");
            return noLetterReply;
        }

        // [NEW] Свій лист, але віддати його зараз не можна — Ink сам пояснить чому:
        // Пантелеймону бракує світла (requiredFlag), Стефанія бере лист лише в розмові (leftFlag)
        LetterData selected = manager.SelectedLetter;
        bool ownLetter = selected != null && selected.AddresseeId == addresseeId;
        if (ownLetter && (!RequiredFlagSet() || !string.IsNullOrEmpty(leftFlag)))
            return StartKnotOr(deliveredKnot, noLetterReply);

        switch (manager.TryDeliver(addresseeId))
        {
            case DeliveryResult.Delivered:
                SetState(NPCState.Received);
                return StartKnotOr(deliveredKnot, deliveredReply);   // [CHANGED]
            case DeliveryResult.WrongAddressee:
                return StartKnotOr(wrongKnot, wrongLetterReply);     // [CHANGED]
            default:
                return noLetterReply;
        }
    }

    // [NEW] Запустити розмову Ink. Якщо Ink у сцені немає (Sandbox_Letters) — повернути просту репліку
    private string StartKnotOr(string knot, string fallback)
    {
        if (string.IsNullOrEmpty(knot) || DialogueManager.Instance == null) return fallback;
        DialogueManager.Instance.StartKnot(knot);
        return null;
    }

    // [NEW]
    private bool RequiredFlagSet() =>
        string.IsNullOrEmpty(requiredFlag) || (GameState.Instance != null && GameState.Instance.GetFlag(requiredFlag));
}
