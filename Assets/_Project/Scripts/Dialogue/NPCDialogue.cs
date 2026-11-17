using UnityEngine;

// Вішається на NPC разом з Interactable (тип Talk): коли Ліля підійшла і клікнула — починається розмова.
[RequireComponent(typeof(Interactable))]
public class NPCDialogue : MonoBehaviour
{
    [Tooltip("Назва вузла в LastLetter_Ch1.ink, напр. panteleimon")]
    [SerializeField] private string knot = "panteleimon";

    private void Awake()
    {
        GetComponent<Interactable>().onInteract.AddListener(Talk);
    }

    public void Talk()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.StartKnot(knot);
    }

    // Інші системи можуть змінити розмову (напр. після доставки листа)
    public void SetKnot(string newKnot) => knot = newKnot;
}
