using UnityEngine;

// Налаштування міні-гри «читання над парою» (механіка M2 з GDD).
// Усі числа тут, а не в коді: змінив значення — онови рядок у GDD.docx тим самим комітом.
[CreateAssetMenu(fileName = "SteamMinigameData", menuName = "LastLetter/Steam Minigame Data")]
public class SteamMinigameData : ScriptableObject
{
    [Header("Колона пари")]
    [Tooltip("На скільки пікселів колона відходить від центру вліво і вправо")]
    public float columnAmplitudePx = 180f;

    [Tooltip("За скільки секунд колона робить повний рух туди й назад")]
    public float columnPeriodSeconds = 3f;

    [Tooltip("Половина ширини колони в пікселях: конверт у цій смузі = конверт у парі")]
    public float columnHalfWidthPx = 60f;

    [Header("Шкала відкриття конверта (від 0 до 1)")]
    [Tooltip("Скільки шкали додається за секунду, поки конверт у парі")]
    public float fillPerSecond = 0.4f;

    [Tooltip("Скільки шкали зникає за секунду, поки конверт поза парою")]
    public float drainPerSecond = 0.2f;

    [Header("Масштаб")]
    [Tooltip("Пікселів на одну одиницю Unity (за GDD — 120)")]
    public float pixelsPerUnit = 120f;

    // Скільки секунд безперервно тримати конверт у парі, щоб відкрити його: 1 / 0.4 = 2.5 с
    public float SecondsToOpen => 1f / fillPerSecond;
}
