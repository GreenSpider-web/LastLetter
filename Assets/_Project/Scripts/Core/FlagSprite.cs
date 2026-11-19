using UnityEngine;

// Показує один із двох спрайтів залежно від прапорця GameState.
// Приклад: ліхтар біля мосту — obj_bridge_lantern_off / obj_bridge_lantern_on за прапорцем lantern_lit.
[RequireComponent(typeof(SpriteRenderer))]
public class FlagSprite : MonoBehaviour
{
    [SerializeField] private string flagName = "lantern_lit";
    [SerializeField] private Sprite spriteOff;      // прапорець false
    [SerializeField] private Sprite spriteOn;       // прапорець true

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged += OnFlagChanged;
        Refresh();      // після «Продовжити» прапорець уже може стояти
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged -= OnFlagChanged;
    }

    private void OnFlagChanged(string name, bool value)
    {
        if (name == flagName) Refresh();
    }

    private void Refresh()
    {
        bool on = GameState.Instance != null && GameState.Instance.GetFlag(flagName);
        spriteRenderer.sprite = on ? spriteOn : spriteOff;
    }
}
