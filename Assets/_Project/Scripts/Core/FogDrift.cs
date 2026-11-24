using UnityEngine;

// Туман Межі: смуга туману повільно гойдається вліво-вправо (GDD → «Атмосфера»: туман над бруківкою).
// Ставить меню LastLetter → «Сцени глави 1: доповнити …». Числа — тут, в Inspector.
public class FogDrift : MonoBehaviour
{
    [Tooltip("На скільки одиниць туман відходить від свого місця")]
    [SerializeField] private float amplitude = 0.8f;

    [Tooltip("За скільки секунд туман робить повний рух туди й назад")]
    [SerializeField] private float period = 24f;

    private Vector3 start;

    private void Start()
    {
        start = transform.position;
    }

    private void Update()
    {
        float x = Mathf.Sin(Time.time * 2f * Mathf.PI / period) * amplitude;
        transform.position = start + new Vector3(x, 0f, 0f);
    }
}
