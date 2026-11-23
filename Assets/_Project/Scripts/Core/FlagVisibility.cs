using UnityEngine;

// Ховає об'єкт, поки стоїть будь-який із прапорців GameState.
// Приклад: пані Стефанія зникає, коли зачинилась у задній кімнаті (tailor_closed) або пішла (tailor_left).
// Висить на окремому об'єкті, бо вимкнений об'єкт уже не почує, що прапорець змінився.
public class FlagVisibility : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private string[] hideFlags = { "tailor_left" };

    private void Start()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged += OnFlagChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged -= OnFlagChanged;
    }

    private void OnFlagChanged(string name, bool value) => Refresh();

    private void Refresh()
    {
        bool hide = false;
        foreach (string flag in hideFlags)
            if (GameState.Instance != null && GameState.Instance.GetFlag(flag)) hide = true;
        target.SetActive(!hide);
    }
}
