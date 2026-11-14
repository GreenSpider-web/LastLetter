using System;
using System.Collections.Generic;
using UnityEngine;

// Стан гри: прапорці сюжету. Поки що мінімальна версія для задачі 2,
// у задачі 14 сюди додасться решта (Втручання, слова Марки тощо).
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    // Прапорці зрізу з GDD (Частина 3 → Програмний контент)
    public static readonly string[] SliceFlags =
    {
        "prologue_done", "read_letter_fisher", "read_letter_tailor",
        "tailor_closed", "tailor_left"
    };

    readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

    // Підписка для інших систем: (назва прапорця, нове значення)
    public event Action<string, bool> OnFlagChanged;

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
}