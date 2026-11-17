using UnityEngine;
using UnityEngine.Events;

// Тип взаємодії визначає курсор і що робить ЛКМ
public enum InteractionType
{
    Examine,    // лише оглянути (лупа)
    Pickup,     // взяти в сумку (рука)
    Use         // використати / натиснути (шестерня)
}

// Об'єкт сцени, з яким можна взаємодіяти: предмет, двері, ліхтар тощо.
// Потрібен Collider2D, щоб його можна було навести мишею.
[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    [Header("Що це")]
    [SerializeField] private string displayName = "Предмет";
    [TextArea(2, 4)]
    [SerializeField] private string description = "Звичайна річ.";     // показується по ПКМ
    [SerializeField] private InteractionType type = InteractionType.Pickup;

    [Header("Для сумки")]
    [Tooltip("Який предмет потрапить у сумку (для типу Pickup)")]
    [SerializeField] private ItemData item;

    [Header("Використання")]
    [TextArea(1, 3)]
    [SerializeField] private string useText = "Нічого не відбувається.";

    [Header("Використання предмета з сумки")]
    [Tooltip("Який предмет треба використати на цьому об'єкті (порожньо — жоден не підходить)")]
    [SerializeField] private ItemData requiredItem;
    [Tooltip("Чи зникає предмет із сумки після використання")]
    [SerializeField] private bool consumeItem = true;
    [SerializeField] private string useSuccessText = "Вийшло.";
    [SerializeField] private string wrongItemText = "Так не вийде.";

    [Header("Події")]
    public UnityEvent onInteract;       // викликається, коли Ліля дійшла і взаємодіяла
    public UnityEvent onItemUsed;       // викликається, коли на об'єкті використали потрібний предмет

    public string DisplayName => displayName;
    public string Description => description;
    public InteractionType Type => type;
    public ItemData Item => item;
    public string UseText => useText;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    // Точка об'єкта, найближча до Лілі (від неї рахуємо 1.2 од.)
    public Vector2 ClosestPointTo(Vector2 from)
    {
        if (col == null) col = GetComponent<Collider2D>();
        return col.ClosestPoint(from);
    }

    // Викликає PlayerInteractor, коли Ліля вже поруч
    public void Interact()
    {
        onInteract?.Invoke();
    }

    // Взяти предмет у сумку. false — сумка повна або не вказано ItemData
    public bool Pickup()
    {
        if (item == null) { Debug.LogWarning($"{name}: не вказано ItemData"); return false; }
        if (InventoryManager.Instance == null || !InventoryManager.Instance.Add(item)) return false;

        onInteract?.Invoke();
        gameObject.SetActive(false);
        return true;
    }

    // Використати предмет із руки на цьому об'єкті. Повертає репліку для гравця
    public string UseItem(ItemData held)
    {
        if (requiredItem == null || held != requiredItem) return wrongItemText;

        if (consumeItem) InventoryManager.Instance.Remove(held);
        else InventoryManager.Instance.Deselect();

        onItemUsed?.Invoke();
        return useSuccessText;
    }

    // Що лежить під точкою світу (верхній за порядком малювання)
    public static Interactable At(Vector2 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        Interactable best = null;
        int bestOrder = int.MinValue;

        foreach (var h in hits)
        {
            var it = h.GetComponentInParent<Interactable>();
            if (it == null || !it.isActiveAndEnabled) continue;

            var sr = it.GetComponentInChildren<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;
            if (order > bestOrder) { best = it; bestOrder = order; }
        }
        return best;
    }
}
