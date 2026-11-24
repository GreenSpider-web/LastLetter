using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Вікно сумки: 12 слотів. Tab або кнопка «Сумка» — відкрити / закрити.
// Клік по предмету — взяти його «в руку»; клік по іншому — поєднати; клік по тому самому — покласти назад.
// З предметом у руці можна закрити сумку й клікнути по об'єкту в сцені (PlayerInteractor).
public class UI_Bag : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] slots;                // 12 кнопок; у кожної діти Icon (Image) і Label (TMP_Text)
    [SerializeField] private Color slotColor = Color.white;                       // колір уже в спрайті ui_slot
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.45f, 1f);  // вибраний — золотавий
    [SerializeField] private AudioClip openSound;           // [NEW] sfx_paper_bag

    private InventoryManager inv;

    private void Start()
    {
        inv = InventoryManager.Instance;
        if (inv == null) { Debug.LogError("UI_Bag: у сцені немає InventoryManager"); return; }

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;      // копія для лямбди
            slots[i].onClick.AddListener(() => OnSlotClick(index));
        }

        inv.OnChanged += Refresh;
        panel.SetActive(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (inv != null) inv.OnChanged -= Refresh;
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) return;
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) Toggle();
    }

    public void Toggle()   // [CHANGED]
    {
        panel.SetActive(!panel.activeSelf);
        AudioManager.PlaySfx(openSound);   // [CHANGED] той самий звук і при відкритті, і при закритті сумки
    }

    private void OnSlotClick(int index)
    {
        if (index >= inv.Items.Count) return;       // порожній слот

        ItemData item = inv.Items[index];
        ItemData held = inv.Selected;

        if (held == null) inv.Select(item);
        else if (held == item) inv.Deselect();
        else Say(inv.Combine(held, item));
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            ItemData item = i < inv.Items.Count ? inv.Items[i] : null;

            var icon = slots[i].transform.Find("Icon").GetComponent<Image>();
            var label = slots[i].transform.Find("Label").GetComponent<TMP_Text>();

            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
            label.text = item != null ? item.displayName : "";

            slots[i].image.color = (item != null && item == inv.Selected) ? selectedColor : slotColor;
        }
    }

    private void Say(string text)
    {
        Debug.Log(text);
        if (InteractionText.Instance != null) InteractionText.Instance.Show(text);
    }
}
