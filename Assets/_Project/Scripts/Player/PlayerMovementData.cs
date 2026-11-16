using UnityEngine;

// Налаштування руху Лілі (GDD → «Властивості гравця», «Вид гравця»).
// Усі числа тут, а не в коді: змінив значення — онови рядок у GDD.docx тим самим комітом.
[CreateAssetMenu(fileName = "PlayerMovementData", menuName = "LastLetter/Player Movement Data")]
public class PlayerMovementData : ScriptableObject
{
    [Header("Рух")]
    [Tooltip("Швидкість ходьби Лілі, од./с (за GDD — 3.5)")]
    public float speed = 3.5f;

    [Header("Масштаб за y (глибина)")]
    [Tooltip("y нижнього краю підлоги — тут Ліля найбільша")]
    public float nearY = -3f;

    [Tooltip("y верхнього краю підлоги — тут Ліля найменша")]
    public float farY = 0f;

    [Tooltip("Масштаб Лілі внизу підлоги")]
    public float nearScale = 1f;

    [Tooltip("Масштаб Лілі вгорі підлоги")]
    public float farScale = 0.7f;
}
