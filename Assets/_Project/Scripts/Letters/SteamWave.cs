using UnityEngine;

// Анімована пара над чайником у вікні «читання над парою» (механіка M2).
// Картинку пари (kettle_steam) натягуємо на сітку з горизонтальних смужок. Щокадру кожна смужка
// трохи зсувається вбік за синусом, і хвиля біжить угору — пара «пливе».
// Біля носика (внизу) пара не рухається, вгорі гойдається найбільше.
// Вліво-вправо всю колону, як і раніше, водить SteamMinigame; цей скрипт лише малює.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SteamWave : MonoBehaviour
{
    [Tooltip("Картинка пари: Art/Items/Cropped/kettle_steam_crop")]
    [SerializeField] private Sprite sprite;

    [Tooltip("Ширина й висота пари в одиницях; низ пари — у точці цього об'єкта (носик чайника)")]
    [SerializeField] private Vector2 size = new Vector2(1.4f, 6.5f);

    [Tooltip("Скільки смужок у сітці: більше — плавніше згинається")]
    [SerializeField] private int rows = 32;

    [Header("Хвиля")]
    [Tooltip("На скільки одиниць гойдається верх пари")]
    [SerializeField] private float amplitude = 0.18f;

    [Tooltip("Довжина хвилі вздовж пари, од.")]
    [SerializeField] private float waveLength = 3f;

    [Tooltip("Скільки хвиль за секунду біжить угору")]
    [SerializeField] private float speed = 0.6f;

    [Tooltip("Наскільки пара ширшає й вужчає (частка ширини)")]
    [SerializeField] private float breathe = 0.08f;

    [Tooltip("До якої частки висоти пара тягнеться від носика: чайник стоїть, а колона ходить вліво-вправо, " +
             "тож низ пари лишається на носику, а вище цієї частки пара вже там, де колона (там і конверт)")]
    [SerializeField] private float anchorUntil = 0.35f;

    [Header("Вигляд")]
    [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private int sortingOrder = 102;     // як у старого прямокутника SteamColumn

    private Mesh mesh;
    private Vector3[] vertices;

    private void Awake()
    {
        GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        BuildMesh();
    }

    private void Update()
    {
        if (mesh != null) UpdateVertices(Time.time);
    }

    // Сітка: на кожен рядок 2 вершини (лівий і правий край), між сусідніми рядками — 2 трикутники
    private void BuildMesh()
    {
        if (sprite == null) { Debug.LogWarning("SteamWave: не задано спрайт пари", this); return; }

        int count = (rows + 1) * 2;
        vertices = new Vector3[count];
        var uv = new Vector2[count];
        var colors = new Color[count];
        var triangles = new int[rows * 6];

        // яка частина текстури — наш спрайт (у 0..1)
        Rect r = sprite.textureRect;
        float w = sprite.texture.width, h = sprite.texture.height;

        for (int i = 0; i <= rows; i++)
        {
            float v = (float)i / rows;                       // 0 — низ, 1 — верх
            float texY = (r.yMin + r.height * v) / h;
            uv[i * 2] = new Vector2(r.xMin / w, texY);
            uv[i * 2 + 1] = new Vector2(r.xMax / w, texY);
            colors[i * 2] = colors[i * 2 + 1] = color;
        }

        for (int i = 0; i < rows; i++)
        {
            int a = i * 2;                                   // лівий низ смужки
            triangles[i * 6 + 0] = a;
            triangles[i * 6 + 1] = a + 2;
            triangles[i * 6 + 2] = a + 1;
            triangles[i * 6 + 3] = a + 1;
            triangles[i * 6 + 4] = a + 2;
            triangles[i * 6 + 5] = a + 3;
        }

        mesh = new Mesh { name = "SteamWave" };
        UpdateVertices(0f);
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // Зсув смужки = синус від (висота − час): гребінь хвилі повзе вгору
    private void UpdateVertices(float time)
    {
        // наскільки колона (батьківський об'єкт) відійшла від чайника, який стоїть у центрі
        float columnX = transform.parent != null ? transform.parent.localPosition.x : 0f;

        for (int i = 0; i <= rows; i++)
        {
            float v = (float)i / rows;
            float y = v * size.y;
            float shift = Mathf.Sin((y / waveLength - time * speed) * 2f * Mathf.PI) * amplitude * v;
            float stick = 1f - Mathf.SmoothStep(0f, 1f, v / anchorUntil);   // 1 біля носика → 0 на висоті anchorUntil
            shift -= columnX * stick;                                        // низ пари тягнеться назад до носика
            float half = size.x * 0.5f * (1f + breathe * Mathf.Sin(time * 1.3f + y));
            vertices[i * 2] = new Vector3(shift - half, y, 0f);
            vertices[i * 2 + 1] = new Vector3(shift + half, y, 0f);
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
}
