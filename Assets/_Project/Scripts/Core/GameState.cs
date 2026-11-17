using System;
using System.Collections.Generic;
using UnityEngine;

// Стан гри: прапорці сюжету і шкала «Втручання».
// DialogueManager синхронізує їх зі змінними Ink (задача 14).
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    // Прапорці зрізу з GDD (Частина 3 → Програмний контент)
    public static readonly string[] SliceFlags =
    {
        "prologue_done", "read_letter_fisher", "read_letter_tailor",
        "tailor_closed", "tailor_left",
        "lantern_lit"           // ліхтар біля мосту запалено (змінна є в LastLetter_Ch1.ink)
    };

    readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

    // Підписка для інших систем: (назва прапорця, нове значення)
    public event Action<string, bool> OnFlagChanged;

    // Шкала «Втручання» (GDD → M7): +1 за кожен лист, прочитаний над парою; у демо 0–2
    public int Intervention { get; private set; }
    public event Action<int> OnInterventionChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var f in SliceFlags) flags[f] = false;
    }

    public bool GetFlag(string name) => flags.TryGetValue(name, out var v) && v;

    public void SetFlag(string name, bool value)
    {
        if (flags.TryGetValue(name, out var old) && old == value) return;
        flags[name] = value;
        Debug.Log($"GameState: {name} = {value}");
        OnFlagChanged?.Invoke(name, value);
    }

    public IEnumerable<string> AllFlags => flags.Keys;

    public void SetIntervention(int value)
    {
        if (Intervention == value) return;
        Intervention = value;
        Debug.Log($"GameState: Втручання = {value}");
        OnInterventionChanged?.Invoke(value);
    }

    public void AddIntervention(int amount) => SetIntervention(Intervention + amount);
}