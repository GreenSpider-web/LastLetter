using UnityEngine;

// Дані одного листа (баланс і контент у ScriptableObject, не в коді).
// У демо два листи: Пантелеймон (fisher) і Стефанія (tailor), див. GDD, механіка M1.
[CreateAssetMenu(fileName = "Letter_New", menuName = "LastLetter/Letter Data")]
public class LetterData : ScriptableObject
{
    [SerializeField] private string letterId = "fisher";
    [SerializeField] private string title = "Лист рибалці";

    [Header("Адресат")]
    [Tooltip("Ідентифікатор адресата; має збігатися з addresseeId в NPCAddressee")]
    [SerializeField] private string addresseeId = "pantelejmon";
    [TextArea(2, 4)]
    [Tooltip("Опис адреси словами, як у GDD (механіка M1): номера будинку немає")]
    [SerializeField] private string addressDescription = "Описова адреса адресата";

    [Header("Читання над парою")]
    [Tooltip("Прапорець з GameState.SliceFlags: read_letter_fisher або read_letter_tailor")]
    [SerializeField] private string readFlag = "read_letter_fisher";

    [Header("Предмет у сумці")]                                                       // [NEW]
    [Tooltip("ItemData цього листа: лист лежить у сумці й вручається як предмет")]   // [NEW]
    [SerializeField] private ItemData item;                                           // [NEW]

    public string LetterId => letterId;
    public string Title => title;
    public string AddresseeId => addresseeId;
    public string AddressDescription => addressDescription;
    public string ReadFlag => readFlag;
    public ItemData Item => item;   // [NEW]
}
