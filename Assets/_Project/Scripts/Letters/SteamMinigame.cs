using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Прототип міні-гри «читання над парою».
// Колона пари хитається вліво-вправо. Гравець веде конверт мишею і тримає його в парі.
// Поки конверт у парі, шкала росте; поза парою — спадає. Шкала = 1 — конверт відкрито.
public class SteamMinigame : MonoBehaviour
{
    [SerializeField] private SteamMinigameData data;
    [SerializeField] private Transform steamColumn;
    [SerializeField] private Transform envelope;

    // Сюди пізніше підключимо «Втручання +1» і відкриття листа
    public UnityEvent onOpened;

    private float progress;       // шкала від 0 до 1
    private float elapsed;        // скільки секунд минуло від старту
    private float openedAfter;    // за скільки секунд відкрили
    private bool isOpened;
    private bool inSteam;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        // R — почати заново (для перевірки відчуття на тестерах)
        if (RestartPressed()) ResetGame();   // [CHANGED]

        if (isOpened) return;

        elapsed += Time.deltaTime;

        // 1. Колона хитається: x = амплітуда * sin(2π * t / період)
        float columnX = PxToUnits(data.columnAmplitudePx)
                        * Mathf.Sin(2f * Mathf.PI * elapsed / data.columnPeriodSeconds);
        steamColumn.localPosition = new Vector3(columnX, steamColumn.localPosition.y, 0f);   // [CHANGED] відносно центру міні-гри

        // 2. Конверт їде за мишею по горизонталі, але не виходить за межі руху колони
        float limit = PxToUnits(data.columnAmplitudePx + data.columnHalfWidthPx);
        float envelopeX = Mathf.Clamp(MouseWorldX() - transform.position.x, -limit, limit);   // [CHANGED] миша відносно центру
        envelope.localPosition = new Vector3(envelopeX, envelope.localPosition.y, 0f);       // [CHANGED]

        // 3. Конверт у парі, якщо він у смузі колони
        inSteam = Mathf.Abs(envelopeX - columnX) <= PxToUnits(data.columnHalfWidthPx);

        // 4. Шкала росте або спадає
        float change = inSteam ? data.fillPerSecond : -data.drainPerSecond;
        progress = Mathf.Clamp01(progress + change * Time.deltaTime);

        // 5. Шкала повна — конверт відкрито
        if (progress >= 1f)
        {
            isOpened = true;
            openedAfter = elapsed;
            Debug.Log("Конверт відкрито за " + openedAfter.ToString("F2") + " с");
            onOpened.Invoke();
        }
    }

    // [NEW] Почати спочатку: R у Sandbox_Steam або UI_Letter.Open у грі.
    // Позиції колони й конверта — відносно цього об'єкта, тож міні-гра працює і в центрі камери (вікно UI_Letter)
    public void ResetGame()
    {
        progress = 0f;
        elapsed = 0f;
        isOpened = false;
    }

    private float PxToUnits(float px)
    {
        return px / data.pixelsPerUnit;
    }

    private float MouseWorldX()
    {
        Vector3 mouse;
#if ENABLE_INPUT_SYSTEM
        mouse = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        mouse = Input.mousePosition;
#endif
        mouse.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(mouse).x;
    }

    private bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    // Тимчасовий вивід на екран для прототипу (справжній HUD зробимо пізніше)
    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 600, 25), "Шкала: " + (progress * 100f).ToString("F0") + "%"
                  + (inSteam ? "  (в парі)" : "  (поза парою)"));
        GUI.Label(new Rect(20, 45, 600, 25), "Час: " + elapsed.ToString("F2") + " с");
        if (isOpened)
            GUI.Label(new Rect(20, 70, 600, 25),
                      "Відкрито за " + openedAfter.ToString("F2") + " с. R — заново");
    }
}
