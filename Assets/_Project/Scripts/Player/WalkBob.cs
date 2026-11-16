using UnityEngine;

// Тимчасова «анімація ходьби» кодом для одного статичного спрайта:
// поки Ліля йде — підстрибує на кожному кроці, злегка хитається і стискається.
// Коли Софія намалює Walk 6 кадрів (задача 19), цей скрипт вимикаємо і вмикаємо Animator.
// Висить на дочірньому об'єкті Visual, а не на Player: Player сам міняє свій масштаб за y.
public class WalkBob : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    [SerializeField] private float stepsPerSecond = 4f;   // скільки кроків (підскоків) за секунду
    [SerializeField] private float bobHeight = 0.08f;     // висота підскоку, од.
    [SerializeField] private float tiltDegrees = 3f;      // нахил уліво-вправо, градуси
    [SerializeField] private float squash = 0.04f;        // стискання в момент кроку (0.04 = 4 %)
    [SerializeField] private float blendSpeed = 8f;       // як швидко анімація з'являється / зникає

    private Vector3 startPos;
    private Vector3 startScale;
    private float phase;    // «час» анімації в радіанах
    private float weight;   // 0 = стоїть, 1 = йде; плавно, щоб не смикалось при зупинці

    private void Awake()
    {
        startPos = transform.localPosition;
        startScale = transform.localScale;
    }

    private void Update()
    {
        float targetWeight = player.IsMoving ? 1f : 0f;
        weight = Mathf.MoveTowards(weight, targetWeight, blendSpeed * Time.deltaTime);

        if (player.IsMoving)
            phase += Time.deltaTime * stepsPerSecond * Mathf.PI;  // |sin| має один горбик на π -> stepsPerSecond підскоків за секунду

        float step = Mathf.Abs(Mathf.Sin(phase));                // 0 = нога на землі, 1 = найвища точка кроку

        // підскок
        transform.localPosition = startPos + Vector3.up * (step * bobHeight * weight);

        // хитання: на одному кроці вліво, на наступному вправо
        float tilt = Mathf.Sin(phase) * tiltDegrees * weight;
        transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // стискання, коли нога торкається землі
        float s = (1f - step) * squash * weight;
        transform.localScale = Vector3.Scale(startScale, new Vector3(1f + s, 1f - s, 1f));
    }
}
