using UnityEngine;
using UnityEngine.InputSystem;

// Тимчасовий текст для задачі 12: назва під курсором і опис / репліка вгорі екрана
// (вгорі — щоб не перекривати вікно діалогу внизу).
// Справжній HUD («підпис під курсором», сповіщення) — задача 25, тоді цей скрипт можна прибрати.
public class InteractionText : MonoBehaviour
{
    public static InteractionText Instance { get; private set; }

    [SerializeField] private float messageSeconds = 3f;
    [SerializeField] private int fontSize = 22;

    private string hoverName;
    private string message;
    private float messageUntil;
    private GUIStyle hoverStyle, messageStyle, shadowStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetHover(string name) => hoverName = name;

    public void Show(string text)
    {
        message = text;
        messageUntil = Time.time + messageSeconds;
    }

    private void OnGUI()
    {
        if (hoverStyle == null)
        {
            hoverStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.UpperCenter };
            hoverStyle.normal.textColor = new Color(0.95f, 0.9f, 0.8f);
            messageStyle = new GUIStyle(hoverStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            shadowStyle = new GUIStyle(messageStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        }

        // назва під курсором
        if (!string.IsNullOrEmpty(hoverName) && Mouse.current != null)
        {
            Vector2 m = Mouse.current.position.ReadValue();
            var r = new Rect(m.x - 150, Screen.height - m.y + 26, 300, 30);
            var sh = new Rect(r.x + 1, r.y + 1, r.width, r.height);
            var hs = new GUIStyle(shadowStyle) { alignment = TextAnchor.UpperCenter, wordWrap = false };
            GUI.Label(sh, hoverName, hs);
            GUI.Label(r, hoverName, hoverStyle);
        }

        // опис / репліка вгорі
        if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
        {
            var box = new Rect(Screen.width * 0.15f, 30, Screen.width * 0.7f, 70);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(box.x + 1, box.y + 1, box.width, box.height), message, shadowStyle);
            GUI.Label(box, message, messageStyle);
        }
    }
}
