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

        delivered.Add(letter.LetterId);
        SelectedLetter = null;
        Debug.Log($"LetterManager: лист '{letter.LetterId}' доставлено ({delivered.Count} з {letters.Length})");
        OnLetterDelivered?.Invoke(letter);
        return DeliveryResult.Delivered;
    }
}
