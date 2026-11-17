using UnityEngine;

// Ставить прапорець у GameState з UnityEvent в Inspector.
// Приклад: Ліхтар → Interactable → On Item Used → GameFlagSetter.Apply (lantern_lit = true).
public class GameFlagSetter : MonoBehaviour
{
    [SerializeField] private string flagName = "lantern_lit";
    [SerializeField] private bool value = true;

    public void Apply()
    {
        if (GameState.Instance != null) GameState.Instance.SetFlag(flagName, value);
    }
}
