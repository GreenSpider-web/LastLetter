using UnityEngine;

// Підлога, по якій ходить Ліля, залежить від прапорця:
// поки пазл ниток не розв'язано — лише до ниток (areaBefore), потім — уся ділянка (areaAfter).
// Перевіряє і на старті сцени, тож після «Продовжити» прохід уже відкритий.
public class FlagWalkArea : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [Tooltip("Напр. puzzle_gate_solved або puzzle_hall_solved")]
    [SerializeField] private string flagName = "puzzle_gate_solved";
    [SerializeField] private Collider2D areaBefore;
    [SerializeField] private Collider2D areaAfter;

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged += OnFlagChanged;
        Refresh();
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
        bool open = GameState.Instance != null && GameState.Instance.GetFlag(flagName);
        player.SetWalkArea(open ? areaAfter : areaBefore);
    }
}
