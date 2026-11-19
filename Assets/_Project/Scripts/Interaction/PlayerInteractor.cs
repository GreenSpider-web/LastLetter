using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Взаємодія Лілі з об'єктами:
//  наведення — курсор (лупа / рука / шестерня) і назва під курсором;
//  ПКМ — опис об'єкта (з будь-якої відстані);
//  ЛКМ — Ліля підходить на 1.2 од. і бере / використовує.
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("З якої відстані Ліля може взяти / використати об'єкт, од. (GDD — 1.2)")]
    [SerializeField] private float interactRange = 1.2f;

    private PlayerController controller;
    private Camera cam;
    private Interactable hovered;
    private Interactable pending;       // до чого Ліля зараз іде

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null || cam == null) return;

        UpdateHover();
        HandleClicks();
        UpdatePending();
    }

    private void UpdateHover()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        hovered = (!overUI && controller.CanMove) ? Interactable.At(MouseWorld()) : null;

        ItemData held = HeldItem();

        if (CursorManager.Instance != null)
        {
            if (hovered != null) CursorManager.Instance.SetFor(held != null ? InteractionType.Use : hovered.Type);
            else CursorManager.Instance.SetDefault();
        }

        if (InteractionText.Instance != null)
        {
            // з предметом у руці: «Запалена свічка → Ліхтар»
            string label = hovered != null ? hovered.DisplayName : null;
            if (held != null) label = hovered != null ? held.displayName + " → " + hovered.DisplayName : held.displayName;
            InteractionText.Instance.SetHover(overUI ? null : label);
        }
    }

    private void HandleClicks()
    {
        // ПКМ з предметом у руці — покласти його назад у сумку
        if (HeldItem() != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            InventoryManager.Instance.Deselect();
            return;
        }

        if (hovered == null) return;

        // ПКМ — оглянути
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Say(hovered.Description);
            return;
        }

        // ЛКМ — підійти і взаємодіяти (рух по землі в цей кадр PlayerController пропускає)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (hovered.Type == InteractionType.Examine && HeldItem() == null)
            {
                Say(hovered.Description);
                return;
            }

            pending = hovered;
            if (InRange(pending)) Finish();
            else controller.WalkTo(pending.ClosestPointTo(transform.position));
        }
    }

    private void UpdatePending()
    {
        if (pending == null) return;

        // поки йшла, об'єкт зник або гравець клікнув по землі
        if (!pending.isActiveAndEnabled) { pending = null; return; }

        if (InRange(pending))
        {
            controller.Stop();
            Finish();
        }
        else if (!controller.IsMoving)
        {
            Say("Не дістати.");
            pending = null;
        }
    }

    private void Finish()
    {
        Interactable it = pending;
        pending = null;

        // предмет у руці — використати його на об'єкті
        ItemData held = HeldItem();
        if (held != null)
        {
            // [NEW] лист адресату або над чайником — вирішує сам об'єкт (IItemReceiver)
            IItemReceiver receiver = it.GetComponent<IItemReceiver>();
            if (receiver != null && receiver.TryReceive(held)) return;

            Say(it.UseItem(held));
            return;
        }

        switch (it.Type)
        {
            case InteractionType.Pickup:
                Say(it.Pickup() ? "Взято: " + it.DisplayName : "Сумка повна.");
                return;
            case InteractionType.Use:
                Say(it.UseText);
                break;
        }
        it.Interact();
    }

    private ItemData HeldItem() => InventoryManager.Instance != null ? InventoryManager.Instance.Selected : null;

    private bool InRange(Interactable it)
    {
        Vector2 me = transform.position;
        return Vector2.Distance(me, it.ClosestPointTo(me)) <= interactRange;
    }

    // Новий клік по землі скасовує похід до предмета
    public void CancelPending() => pending = null;

    private Vector2 MouseWorld() => cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

    private void Say(string text)
    {
        Debug.Log(text);
        if (InteractionText.Instance != null) InteractionText.Instance.Show(text);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
