using UnityEngine;

// Предмет, який може лежати в сумці (GDD → M4 «Сумка листоноші»).
// На кожен предмет зрізу — свій асет у ScriptableObjects/Items.
[CreateAssetMenu(fileName = "Item", menuName = "LastLetter/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("Назва для коду і збереження, напр. item_candle")]
    public string id;

    [Tooltip("Назва, яку бачить гравець")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Іконка 128×128 (задача 30). Поки порожньо — у сумці видно лише назву")]
    public Sprite icon;
}
