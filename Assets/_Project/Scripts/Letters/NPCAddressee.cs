using System;
using UnityEngine;

// Стани адресата. Closed і Left потрібні для Стефанії (прапорці tailor_closed / tailor_left, GDD);
// у цій задачі їх ставимо вручну через SetState.
public enum NPCState
{
    Waiting,    // чекає на лист
    Received,   // лист уже отримав
    Closed,     // двері зачинені
    Left        // пішов
}

// Адресат листа: відповідає на вручення залежно від стану й від того, чи лист його.
// Тексти відповідей поки прості поля; пізніше їх замінять діалоги Ink.
public class NPCAddressee : MonoBehaviour
{
    [SerializeField] private string addresseeId = "pantelejmon";
    [SerializeField] private NPCState state = NPCState.Waiting;

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
        Debug.Log($"NPC {addresseeId}: {reply}");
        OnReply?.Invoke(reply);
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

        switch (manager.TryDeliver(addresseeId))
        {
            case DeliveryResult.Delivered:
                SetState(NPCState.Received);
                return deliveredReply;
            case DeliveryResult.WrongAddressee:
                return wrongLetterReply;
            default:
                return noLetterReply;
        }
    }
}
