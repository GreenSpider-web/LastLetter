using UnityEngine;

// Чайник SteamKettle (1-А, x = 6, механіка M2 з GDD).
// Без листа в руці — Марка коментує чайник (вузол Ink marka_kettle).
// З листом у руці — відкривається вікно пари (UI_Letter), де лист можна прочитати.
[RequireComponent(typeof(Interactable))]
public class SteamKettle : MonoBehaviour, IItemReceiver
{
    [SerializeField] private UI_Letter letterWindow;

    [Tooltip("Вузол Ink, коли Ліля підходить до чайника без листа")]
    [SerializeField] private string approachKnot = "marka_kettle";

    [SerializeField] private string alreadyOpenText = "Цей конверт уже розпечатано.";

    private void Awake()
    {
        GetComponent<Interactable>().onInteract.AddListener(OnApproach);
    }

    private void OnApproach()
    {
        if (DialogueManager.Instance != null && !string.IsNullOrEmpty(approachKnot))
            DialogueManager.Instance.StartKnot(approachKnot);
    }

    // Ліля використала предмет із сумки на чайнику
    public bool TryReceive(ItemData item)
    {
        LetterManager letters = LetterManager.Instance;
        LetterData letter = letters != null ? letters.FindByItem(item) : null;
        if (letter == null) return false;       // не лист — звичайна відповідь Interactable

        InventoryManager.Instance.Deselect();

        bool alreadyRead = GameState.Instance != null && GameState.Instance.GetFlag(letter.ReadFlag);
        if (alreadyRead)
        {
            if (InteractionText.Instance != null) InteractionText.Instance.Show(alreadyOpenText);
            return true;
        }

        letterWindow.Open(letter);
        return true;
    }
}
