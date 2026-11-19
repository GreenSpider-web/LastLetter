using System;
using System.Collections.Generic;
using UnityEngine;

// Сумка листоноші: які предмети має Ліля, який предмет зараз «у руці», поєднання.
// Один на всю гру (переходить між сценами), як і GameState.
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Tooltip("Кількість слотів сумки (GDD → Ігрова матриця: 12)")]
    [SerializeField] private int maxSlots = 12;

    [Tooltip("Рецепти поєднання (у зрізі — 1)")]
    [SerializeField] private List<CombineRecipe> recipes = new List<CombineRecipe>();

    [Tooltip("Репліка, коли предмети не поєднуються")]
    [SerializeField] private string failText = "Це не поєднується.";

    [Tooltip("Усі предмети гри — щоб при завантаженні знайти предмет за id")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    private readonly List<ItemData> items = new List<ItemData>();

    // Що вже підібрано з підлоги: «Сцена/Об'єкт». Щоб після «Продовжити» предмет не лежав знову
    private readonly HashSet<string> pickedUp = new HashSet<string>();
    public IEnumerable<string> PickedUp => pickedUp;

    public IReadOnlyList<ItemData> Items => items;
    public int MaxSlots => maxSlots;

    // Предмет, вибраний у сумці: його можна поєднати з іншим або використати на об'єкті
    public ItemData Selected { get; private set; }

    // UI_Bag підписується, щоб перемалюватися
    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool Has(ItemData item) => items.Contains(item);

    public void MarkPickedUp(string key) => pickedUp.Add(key);
    public bool WasPickedUp(string key) => pickedUp.Contains(key);

    // Нова гра — порожня сумка
    public void Clear()
    {
        items.Clear();
        pickedUp.Clear();
        Selected = null;
        OnChanged?.Invoke();
    }

    // «Продовжити» — сумка зі збереження
    public void LoadState(List<string> itemIds, List<string> picked)
    {
        items.Clear();
        pickedUp.Clear();
        Selected = null;

        foreach (string id in itemIds)
        {
            ItemData item = allItems.Find(i => i != null && i.id == id);
            if (item != null) items.Add(item);
            else Debug.LogWarning($"InventoryManager: немає ItemData з id «{id}» у списку allItems");
        }
        foreach (string key in picked) pickedUp.Add(key);

        OnChanged?.Invoke();
    }

    public bool Add(ItemData item)
    {
        if (item == null || items.Count >= maxSlots) return false;
        items.Add(item);
        Debug.Log("Сумка: + " + item.displayName);
        OnChanged?.Invoke();
        return true;
    }

    public void Remove(ItemData item)
    {
        if (!items.Remove(item)) return;
        if (Selected == item) Selected = null;
        Debug.Log("Сумка: − " + item.displayName);
        OnChanged?.Invoke();
    }

    public void Select(ItemData item)
    {
        Selected = item;
        OnChanged?.Invoke();
    }

    public void Deselect()
    {
        if (Selected == null) return;
        Selected = null;
        OnChanged?.Invoke();
    }

    // Поєднати два предмети. Повертає репліку для гравця.
    public string Combine(ItemData a, ItemData b)
    {
        foreach (CombineRecipe recipe in recipes)
        {
            if (!recipe.Matches(a, b)) continue;

            Remove(a);
            Remove(b);
            Add(recipe.result);
            Selected = null;
            OnChanged?.Invoke();
            return recipe.successText;
        }
        return failText;
    }
}
