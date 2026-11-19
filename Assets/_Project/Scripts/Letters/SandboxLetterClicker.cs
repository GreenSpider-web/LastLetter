using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Тестовий стенд для задачі 15 (без UI): кнопки обирають лист, клік мишею по NPC вручає його.
// Це не частина гри: у справжній сцені вручення піде через Interactable та інвентар.
public class SandboxLetterClicker : MonoBehaviour
{
    private string lastReply = "";

    private void Update()
    {
        if (!MouseClickedThisFrame()) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(MousePosition());
        Collider2D hit = Physics2D.OverlapPoint(world);
        if (hit == null) return;

        NPCAddressee npc = hit.GetComponent<NPCAddressee>();
        if (npc != null) lastReply = npc.AddresseeId + ": " + npc.Interact();
    }

    private void OnGUI()
    {
        LetterManager manager = LetterManager.Instance;
        if (manager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 520, 400));
        GUILayout.Label("Оберіть лист, потім клікніть по NPC");

        foreach (LetterData letter in manager.Letters)
        {
            string mark = manager.IsDelivered(letter) ? " [доставлено]" : "";
            if (manager.SelectedLetter == letter) mark += " <-- обрано";
            if (GUILayout.Button(letter.Title + " (адресат: " + letter.AddresseeId + ")" + mark))
                manager.SelectLetter(letter);
        }

        GUILayout.Label("Доставлено: " + manager.DeliveredCount + " з " + manager.Letters.Count);
        GUILayout.Label("Остання репліка: " + lastReply);
        GUILayout.EndArea();
    }

    private static bool MouseClickedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static Vector3 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }
}
