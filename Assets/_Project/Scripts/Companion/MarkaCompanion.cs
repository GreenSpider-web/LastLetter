using UnityEngine;

// Марка — паперовий журавлик-супутник (GDD: Companion_Marka). Летить сам, гравець ним не керує.
// Висить за спиною Лілі трохи вище середини її голови і злегка погойдується.
// Коли Ліля розвертається, Марка перелітає дугою над її головою на інший бік — знову за спину.
// Об'єкт лежить у префабі Player (тому Марка є в кожній сцені з Лілею), але на старті від'єднується:
// інакше він рухався б разом із Лілею миттєво, без плавного запізнення.
// Підказки раз на 20 с і світіння (задача 27) додаються окремим скриптом на цей самий об'єкт.
public class MarkaCompanion : MonoBehaviour
{
    [Header("Посилання")]
    [SerializeField] private PlayerController player;           // якщо порожньо — знайде сам
    [SerializeField] private SpriteRenderer spriteRenderer;     // спрайт Марки (marka_fly)

    [Header("Де летить")]
    [Tooltip("Висота як частка зросту Лілі: 0.85 — середина голови, 0.88 — трохи вище")]
    [SerializeField, Range(0.5f, 1.2f)] private float heightOnLilia = 0.88f;
    [Tooltip("Наскільки за спиною, од. (коли масштаб Лілі = 1)")]
    [SerializeField] private float behindDistance = 1.0f;
    [Tooltip("Ширина Марки, од. (коли масштаб Лілі = 1)")]
    [SerializeField] private float width = 0.8f;

    [Header("Політ")]
    [SerializeField] private float smoothTime = 0.15f;          // запізнення за Лілею, с (щоб виглядало «живим»)
    [SerializeField] private float arcHeight = 0.6f;            // наскільки піднімається, перелітаючи над головою, од.
    [SerializeField] private float bobHeight = 0.08f;           // погойдування вгору-вниз, од.
    [SerializeField] private float bobPeriod = 1.6f;            // секунд на одне погойдування
    [SerializeField] private bool spriteFacesLeft = true;       // на малюнку marka_fly дзьоб дивиться вліво

    private SpriteRenderer lilia;       // спрайт Лілі: з нього беремо її зріст і куди вона дивиться
    private Vector3 followPos;          // плавна точка за спиною (без дуги й погойдування)
    private Vector3 velocity;           // потрібна для SmoothDamp
    private float baseScale;            // масштаб, при якому Марка має ширину width
    private SpriteRenderer glow;        // [NEW] світіння за Маркою, коли підказка готова (HintSystem)

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();

        transform.SetParent(null, true);    // летимо самі, а не «приклеєні» до Лілі
    }

    private void Start()
    {
        if (player == null || player.Sprite == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("MarkaCompanion: не знайшов Лілю або спрайт Марки — Марка не летить.");
            enabled = false;
            return;
        }

        lilia = player.Sprite;
        baseScale = width / spriteRenderer.sprite.bounds.size.x;

        // на 1 вище за Лілю, щоб фон ніколи не перекрив Марку;
        // над головою дуга піднімає його вгору, тож обличчя Лілі він не закриває
        spriteRenderer.sortingLayerID = lilia.sortingLayerID;
        spriteRenderer.sortingOrder = lilia.sortingOrder + 1;

        followPos = BehindPoint();          // одразу на місці, без перельоту через усю сцену
        LateUpdate();
    }

    private void LateUpdate()
    {
        float liliaScale = player.transform.localScale.x;   // Ліля менша, коли йде вглиб, — Марка теж

        followPos = Vector3.SmoothDamp(followPos, BehindPoint(), ref velocity, smoothTime);

        // side: −1 або 1 — Марка за спиною, 0 — прямо над Лілею. Над головою дуга найвища.
        float side = (followPos.x - lilia.bounds.center.x) / (behindDistance * liliaScale);
        float lift = arcHeight * liliaScale * Mathf.Clamp01(1f - side * side);
        float bob = Mathf.Sin(Time.time * 2f * Mathf.PI / bobPeriod) * bobHeight * liliaScale;

        transform.position = followPos + new Vector3(0f, lift + bob, 0f);
        transform.localScale = Vector3.one * baseScale * liliaScale;

        // дивиться туди ж, куди Ліля
        spriteRenderer.flipX = spriteFacesLeft ? !lilia.flipX : lilia.flipX;
    }

    // [NEW] Світіння «підказка готова»: м'яка пляма за журавликом, що повільно «дихає»
    public void SetGlow(bool on, Color color, float period)
    {
        if (glow == null)
        {
            if (!on || spriteRenderer == null) return;
            glow = CreateGlow();
        }

        glow.enabled = on;
        if (!on) return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f * Mathf.PI / period);   // 0…1
        glow.color = new Color(color.r, color.g, color.b, color.a * Mathf.Lerp(0.35f, 1f, pulse));
    }

    // [NEW] Дочірній спрайт-пляма (малюється кодом, поки немає арту), трохи більший за Марку
    private SpriteRenderer CreateGlow()
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * spriteRenderer.sprite.bounds.size.x * 1.3f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UIBuild.DrawSprite(64, (x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y))));
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;   // за журавликом
        return sr;
    }

    // Точка за спиною Лілі трохи вище середини голови
    private Vector3 BehindPoint()
    {
        Bounds b = lilia.bounds;
        float back = lilia.flipX ? 1f : -1f;    // flipX = Ліля дивиться вліво, отже спина справа
        float x = b.center.x + back * behindDistance * player.transform.localScale.x;
        float y = b.min.y + b.size.y * heightOnLilia;
        return new Vector3(x, y, lilia.transform.position.z);
    }
}
