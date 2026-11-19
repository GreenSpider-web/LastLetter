using UnityEngine;

// Читання листа над парою: зв'язує міні-гру з наслідками (механіки M2 і M7 з GDD).
// Коли конверт відкрито: печатка ламається, Втручання +1, ставиться прапорець read_letter_*.
// Один лист дає Втручання лише раз: повторне читання шкалу не збільшує.
public class SteamReader : MonoBehaviour
{
    [SerializeField] private SteamMinigame minigame;

    [Header("Печатка на конверті")]
    [SerializeField] private GameObject sealIntact;   // ціла печатка
    [SerializeField] private GameObject sealBroken;   // зламана печатка

    [Header("Лист")]
    [Tooltip("Який лист читаємо: прапорець read_letter_* береться з нього. У грі лист передає UI_Letter")]   // [CHANGED]
    [SerializeField] private LetterData letter;   // [CHANGED] було string readFlag — тепер назва прапорця лише в LetterData

    private string ReadFlag => letter != null ? letter.ReadFlag : "";   // [NEW]

    // [NEW] UI_Letter передає лист, який Ліля принесла до чайника
    public void SetLetter(LetterData newLetter)
    {
        letter = newLetter;
        ShowSeal(IsRead());
    }

    // [NEW] Чи цей лист уже читали
    private bool IsRead() => GameState.Instance != null && GameState.Instance.GetFlag(ReadFlag);

    private void OnEnable()
    {
        minigame.onOpened.AddListener(OnEnvelopeOpened);
    }

    private void OnDisable()
    {
        minigame.onOpened.RemoveListener(OnEnvelopeOpened);
    }

    private void Start()
    {
        // Якщо лист уже читали раніше, печатка від початку зламана
        ShowSeal(IsRead());   // [CHANGED]
    }

    private void OnEnvelopeOpened()
    {
        ShowSeal(true);

        GameState state = GameState.Instance;
        if (state == null)
        {
            Debug.LogWarning("SteamReader: у сцені немає GameState, Втручання не нараховано");
            return;
        }

        if (letter == null) { Debug.LogWarning("SteamReader: не вказано лист"); return; }   // [NEW]
        if (state.GetFlag(ReadFlag)) return;   // цей лист уже читали: +1 вдруге не даємо   // [CHANGED]

        state.SetFlag(ReadFlag, true);   // [CHANGED]
        state.AddIntervention(1);
    }

    private void ShowSeal(bool broken)
    {
        sealIntact.SetActive(!broken);
        sealBroken.SetActive(broken);
    }

#if UNITY_EDITOR
    // Лише для тесту в редакторі: шкала Втручання для гравця прихована (GDD, M7)
    private void OnGUI()
    {
        if (GameState.Instance == null) return;
        GUI.Label(new Rect(20, 95, 600, 25),
                  "Втручання: " + GameState.Instance.Intervention + " / " + 2
                  + "   " + ReadFlag + " = " + GameState.Instance.GetFlag(ReadFlag));   // [CHANGED]
    }
#endif
}
