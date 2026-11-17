using UnityEngine;

// Міняє курсор залежно від того, що під мишею (GDD: курсори Kenney Cursor Pack, поки — тимчасові).
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Курсори 32×32 (Texture Type = Cursor). Порожнє поле — системна стрілка")]
    [SerializeField] private Texture2D examineCursor;   // лупа
    [SerializeField] private Texture2D takeCursor;      // рука
    [SerializeField] private Texture2D useCursor;       // шестерня
    [SerializeField] private Texture2D talkCursor;      // бульбашка

    private Texture2D current;
    private bool hasCurrent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetDefault() => Apply(null, Vector2.zero);

    public void SetFor(InteractionType type)
    {
        switch (type)
        {
            case InteractionType.Examine: Apply(examineCursor, new Vector2(12, 12)); break;
            case InteractionType.Pickup:  Apply(takeCursor,    new Vector2(14, 4));  break;
            case InteractionType.Use:     Apply(useCursor,     new Vector2(16, 16)); break;
            case InteractionType.Talk:    Apply(talkCursor,    new Vector2(5, 29));  break;
        }
    }

    private void Apply(Texture2D tex, Vector2 hotspot)
    {
        if (hasCurrent && tex == current) return;   // не викликати SetCursor кожен кадр
        current = tex;
        hasCurrent = true;
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }

    private void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
