using System;
using System.Collections.Generic;
using UnityEngine;

// Результат спроби вручити лист
public enum DeliveryResult
{
    NoLetterSelected,   // гравець не обрав лист
    WrongAddressee,     // лист не для цього адресата
    Delivered           // лист вручено
}

// Тримає листи, які несе гравець, обраний лист і список доставлених (механіка M1 з GDD).
// Одинак на кшталт GameState: живе між сценами.
public class LetterManager : MonoBehaviour
{
    public static LetterManager Instance { get; private set; }

    [SerializeField] private LetterData[] letters;   // усі листи демо

    private readonly HashSet<string> delivered = new HashSet<string>();

    public LetterData SelectedLetter { get; private set; }
    public IReadOnlyList<LetterData> Letters => letters;
    public int DeliveredCount => delivered.Count;

    public event Action<LetterData> OnLetterSelected;
    public event Action<LetterData> OnLetterDelivered;
    public event Action<LetterData, string> OnWrongAddressee;   // лист, id адресата, якому його віддали

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsDelivered(LetterData letter) => letter != null && delivered.Contains(letter.LetterId);

    // [NEW] Який лист лежить у сумці як цей предмет (null — предмет не лист)
    public LetterData FindByItem(ItemData item)
    {
        if (item == null) return null;
        foreach (LetterData letter in letters)
            if (letter != null && letter.Item == item) return letter;
        return null;
    }

    // [NEW] Лист для цього адресата (null — йому листа немає)
    public LetterData FindByAddressee(string addresseeId)
    {
        foreach (LetterData letter in letters)
            if (letter != null && letter.AddresseeId == addresseeId) return letter;
        return null;
    }

    // [NEW] Для збереження: id доставлених листів
    public IEnumerable<string> DeliveredIds => delivered;

    // [NEW] «Продовжити» — доставлені листи зі збереження
    public void LoadState(List<string> deliveredIds)
    {
        delivered.Clear();
        if (deliveredIds != null)
            foreach (string id in deliveredIds) delivered.Add(id);
        SelectedLetter = null;
    }

    // [NEW] «Нова гра» — жоден лист не доставлено
    public void Clear()
    {
        delivered.Clear();
        SelectedLetter = null;
    }

    // Обрати лист, який гравець хоче вручити (null знімає вибір)
    public void SelectLetter(LetterData letter)
    {
        SelectedLetter = letter;
        OnLetterSelected?.Invoke(letter);
    }

    // Спроба вручити обраний лист адресату з таким id
    public DeliveryResult TryDeliver(string addresseeId)
    {
        LetterData letter = SelectedLetter;
        if (letter == null || IsDelivered(letter))
            return DeliveryResult.NoLetterSelected;

        if (letter.AddresseeId != addresseeId)
        {
            Debug.Log($"LetterManager: лист '{letter.LetterId}' не для '{addresseeId}'");
            OnWrongAddressee?.Invoke(letter, addresseeId);
            return DeliveryResult.WrongAddressee;
        }

        MarkDelivered(letter);   // [CHANGED]
        return DeliveryResult.Delivered;
    }

    // [NEW] Позначити лист доставленим і прибрати його із сумки.
    // Викликає TryDeliver, а також NPCAddressee, коли лист забирають у діалозі Ink (Стефанія)
    public void MarkDelivered(LetterData letter)
    {
        if (letter == null || IsDelivered(letter)) return;

        delivered.Add(letter.LetterId);
        if (SelectedLetter == letter) SelectedLetter = null;
        if (InventoryManager.Instance != null && letter.Item != null) InventoryManager.Instance.Remove(letter.Item);

        Debug.Log($"LetterManager: лист '{letter.LetterId}' доставлено ({delivered.Count} з {letters.Length})");
        OnLetterDelivered?.Invoke(letter);
    }
}
