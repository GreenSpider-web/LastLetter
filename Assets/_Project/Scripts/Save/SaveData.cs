using System;
using System.Collections.Generic;

// Що лежить у файлі автозбереження (save.json). JsonUtility не вміє словники, тому — списки.
[Serializable]
public class SaveData
{
    public string scene;                                    // ділянка, де зберегли
    public float playerX;                                   // де стояла Ліля
    public float playerY;
    public List<FlagEntry> flags = new List<FlagEntry>();   // прапорці GameState
    public int intervention;                                // шкала «Втручання»
    public List<string> items = new List<string>();         // id предметів у сумці
    public List<string> pickedUp = new List<string>();      // «Сцена/Об'єкт» — що вже підібрано з підлоги
    public List<string> delivered = new List<string>();     // [NEW] id доставлених листів (LetterManager)
}

[Serializable]
public class FlagEntry
{
    public string name;
    public bool value;
}
