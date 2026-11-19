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
    [Tooltip("Прапорець з GameState.SliceFlags: read_letter_fisher або read_letter_tailor")]
    [SerializeField] private string readFlag = "read_letter_fisher";

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
        bool alreadyRead = GameState.Instance != null && GameState.Instance.GetFlag(readFlag);
        ShowSeal(alreadyRead);
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

        if (state.GetFlag(readFlag)) return;   // цей лист уже читали: +1 вдруге не даємо

        state.SetFlag(readFlag, true);
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
                  + "   " + readFlag + " = " + GameState.Instance.GetFlag(readFlag));
    }
#endif
}
