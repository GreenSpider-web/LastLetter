using System.Collections;
using UnityEngine;

// Мешканець «переходить міст» (GDD: «Пантелеймон іде за міст»).
// Коли з'являється прапорець (Пантелеймон — prologue_done) і розмова закінчилась:
// встає (спрайт «іде»), іде точками шляху до мосту й по ньому, меншає (іде вдалину), тане і зникає.
// Ходьба — кадри walkFrames по колу. Якщо кадрів немає — один спрайт walkSprite і крок кодом (підстрибує й хитається).
// Якщо прапорець уже стоїть (Продовжити) — мешканця на місці просто немає.
public class NPCWalkAway : MonoBehaviour
{
    [SerializeField] private string leaveFlag = "prologue_done";

    [Header("Вигляд")]
    [Tooltip("Спрайт стоячого / ходячого (точка опори — під ногами)")]
    [SerializeField] private Sprite walkSprite;
    [Tooltip("Масштаб стоячого спрайта (однаковий по x і y)")]
    [SerializeField] private float walkScale = 0.25f;
    [Tooltip("У кінці шляху — частка від walkScale (іде вдалину)")]
    [SerializeField] private float farScale = 0.65f;
    [Tooltip("Кадри ходьби по порядку (точка опори — під ногами)")]
    [SerializeField] private Sprite[] walkFrames;
    [Tooltip("Масштаб кадрів ходьби (вони менші за walkSprite)")]
    [SerializeField] private float framesScale = 0.66f;
    [SerializeField] private float framesPerSecond = 6f;
    [Tooltip("Спрайт намальований обличчям праворуч")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Шлях (де ноги), останній — уже за кадром")]
    [SerializeField] private Vector2[] path =
    {
        new Vector2(19.2f, -3.88f), new Vector2(16.9f, -2.35f), new Vector2(19.3f, -1.35f), new Vector2(22.8f, -0.25f)
    };

    [Header("Рух")]
    [SerializeField] private float standUpDelay = 0.7f;   // встає, перш ніж піти
    [SerializeField] private float speed = 1f;            // од./с — повільно, він старий
    [SerializeField] private float stepsPerSecond = 1.6f;
    [SerializeField] private float stepHeight = 0.07f;
    [SerializeField] private float stepTilt = 3f;         // градусів
    [Tooltip("З якої частини шляху починає танути (0…1)")]
    [SerializeField] private float fadeFrom = 0.7f;

    private SpriteRenderer sr;
    private bool leaving;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        GameState gs = GameState.Instance;
        if (gs == null) return;

        if (gs.GetFlag(leaveFlag)) { Debug.Log("NPCWalkAway: прапорець уже стоїть, прибираю на старті"); gameObject.SetActive(false); return; }   // [CHANGED] лог   // уже пішов раніше
        gs.OnFlagChanged += OnFlagChanged;
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null) GameState.Instance.OnFlagChanged -= OnFlagChanged;
    }

    private void OnFlagChanged(string flag, bool value)
    {
        if (flag == leaveFlag && value && !leaving) { Debug.Log("NPCWalkAway: пішов"); StartCoroutine(Leave()); }   // [CHANGED] лог
    }

    private IEnumerator Leave()
    {
        leaving = true;

        // дочитати діалог до кінця
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) yield return null;

        // більше не клікається
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;

        // встає
        bool hasFrames = walkFrames != null && walkFrames.Length > 0;
        float baseScale = hasFrames ? framesScale : walkScale;
        sr.sprite = hasFrames ? walkFrames[0] : walkSprite;
        transform.localScale = Vector3.one * baseScale;
        transform.position = path[0];
        yield return new WaitForSeconds(standUpDelay);

        float total = 0f;
        for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);

        float walked = 0f;
        float time = 0f;
        Vector2 pos = path[0];
        Color color = sr.color;

        for (int i = 1; i < path.Length; i++)
        {
            Vector2 target = path[i];
            sr.flipX = spriteFacesRight ? target.x < pos.x : target.x > pos.x;

            while (pos != target)
            {
                Vector2 next = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
                walked += Vector2.Distance(pos, next);
                pos = next;
                time += Time.deltaTime;

                float progress = walked / total;
                transform.localScale = Vector3.one * baseScale * Mathf.Lerp(1f, farScale, progress);

                if (hasFrames)
                {
                    sr.sprite = walkFrames[(int)(time * framesPerSecond) % walkFrames.Length];
                    float bob = Mathf.Abs(Mathf.Sin(time * framesPerSecond * Mathf.PI / walkFrames.Length * 2f));
                    transform.position = pos + Vector2.up * bob * stepHeight;   // підстрибує на кожному кроці
                }
                else
                {
                    float step = Mathf.Sin(time * stepsPerSecond * Mathf.PI);
                    transform.position = pos + Vector2.up * Mathf.Abs(step) * stepHeight;   // підстрибує на кожному кроці
                    transform.rotation = Quaternion.Euler(0f, 0f, step * stepTilt);         // і трохи хитається
                }

                color.a = progress < fadeFrom ? 1f : Mathf.InverseLerp(1f, fadeFrom, progress);
                sr.color = color;
                yield return null;
            }
        }

        Debug.Log("NPCWalkAway: дійшов, зникає");   // [NEW]
        gameObject.SetActive(false);
    }
}
