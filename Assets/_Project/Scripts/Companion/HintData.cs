using UnityEngine;

// Налаштування підказок Марки (GDD → M6, «Властивості гравця» → Підказка).
// Змінив число — онови рядок у GDD.docx тим самим комітом.
[CreateAssetMenu(fileName = "HintData", menuName = "LastLetter/Hint Data")]
public class HintData : ScriptableObject
{
    [Tooltip("Перезарядка підказки, с (GDD — 20)")]
    public float hintCooldown = 20f;

    [Tooltip("Колір світіння Марки, коли підказка готова")]
    public Color glowColor = new Color(1f, 0.85f, 0.45f, 0.55f);

    [Tooltip("Секунд на одне «дихання» світіння")]
    public float glowPeriod = 1.4f;
}
