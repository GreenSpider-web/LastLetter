using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Вікно пазла ниток (задача 23): сітка з ThreadPuzzleData, клік повертає плитку на 90°,
// нитки від голки світяться своїм кольором, після розв'язку нитки падають і вікно закривається.
// Поки немає малюнків плиток (задача 24), нитки малюються простими смужками з Image.
// ПКМ або кнопка «Закрити» — вийти, не розв'язавши (повороти зберігаються до наступного відкриття).
public class ThreadPuzzleUI : MonoBehaviour
{
    [SerializeField] private GameObject window;
    [SerializeField] private RectTransform board;      // сюди будуються плитки
    [SerializeField] private Text title;
    [SerializeField] private Button closeButton;

    [Header("Розміри (GDD: плитки 128×128)")]
    [SerializeField] private float cellSize = 128f;
    [SerializeField] private float threadWidth = 14f;

    [Header("Кольори")]
    [SerializeField] private Color tileColor = new Color(0.2f, 0.18f, 0.16f);
    [SerializeField] private Color darkThread = new Color(0.4f, 0.37f, 0.33f);   // нитка, до якої ще не дійшло
    [SerializeField] private Color redThread = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color whiteThread = new Color(0.95f, 0.94f, 0.9f);
    [SerializeField] private Color hintColor = new Color(0.85f, 0.7f, 0.2f);

    [Header("Звук і падіння")]
    [SerializeField] private AudioClip rotateSound;     // шурхіт ниток (коли з'явиться)
    [SerializeField] private AudioClip solvedSound;
    [Tooltip("Скільки секунд падають нитки після розв'язку")]
    [SerializeField] private float fallTime = 0.8f;

    // Одна плитка на екрані
    private class TileView
    {
        public Image background;
        public RectTransform content;       // його повертаємо разом із нитками
        public Image[] segments = new Image[4];   // смужка до кожної сторони (до повороту); null — немає
        public Image knob;                  // кружечок у центрі
    }

    private ThreadPuzzleData data;
    private int[] rotations;
    private TileView[] tiles;
    private readonly List<Image> spools = new List<Image>();
    private Action onSolved;
    private bool busy;                      // нитки падають — кліки не приймаємо
    private PlayerController player;

    public bool IsOpen => window.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        window.SetActive(false);
    }

    // Відкрити пазл. solvedCallback викликається, коли пазл розв'язано
    public void Open(ThreadPuzzleData puzzle, Action solvedCallback)
    {
        if (puzzle == null) { Debug.LogError("ThreadPuzzleUI: не вказано ThreadPuzzleData"); return; }

        if (puzzle != data || rotations == null)
        {
            data = puzzle;
            rotations = data.StartRotations();
            Build();
        }

        onSolved = solvedCallback;
        busy = false;
        title.text = data.Title;
        window.SetActive(true);

        player = FindAnyObjectByType<PlayerController>();
        if (player != null) { player.Stop(); player.CanMove = false; }

        Refresh();
    }

    public void Close()
    {
        if (busy) return;
        window.SetActive(false);
        if (player != null) player.CanMove = true;
    }

    private void Update()
    {
        if (!IsOpen || PauseMenu.IsPaused) return;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Close();
    }

    // ---------- Побудова сітки ----------

    private void Build()
    {
        foreach (Transform child in board) Destroy(child.gameObject);
        spools.Clear();

        board.sizeDelta = new Vector2(data.Width * cellSize, data.Height * cellSize);
        tiles = new TileView[data.TileCount];

        for (int y = 0; y < data.Height; y++)
            for (int x = 0; x < data.Width; x++)
                tiles[y * data.Width + x] = BuildTile(x, y);

        // голки й котушки за краєм сітки
        foreach (ThreadLine line in data.Lines)
        {
            Color c = ColorOf(line.color);
            MakeMarker("Needle", line.needleCell, (int)line.needleSide, new Vector2(threadWidth, 44f), c);
            spools.Add(MakeMarker("Spool", line.spoolCell, (int)line.spoolSide, new Vector2(40f, 40f), Dim(c)));
        }
    }

    private TileView BuildTile(int x, int y)
    {
        int index = y * data.Width + x;
        ThreadTile tile = data.Tile(index);
        var view = new TileView();

        RectTransform rt = NewRect("Tile_" + x + "_" + y, board);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);       // від лівого верхнього кута
        rt.sizeDelta = new Vector2(cellSize - 4f, cellSize - 4f);
        rt.anchoredPosition = CellCenter(x, y);
        view.background = rt.gameObject.AddComponent<Image>();
        view.background.color = tileColor;

        if (tile.type != ThreadTileType.Empty)
        {
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnTileClicked(index));
        }

        view.content = NewRect("Content", rt);
        view.content.sizeDelta = rt.sizeDelta;

        // смужки до сторін плитки в положенні «до повороту»
        int mask = ThreadPuzzleSolver.BaseMask(tile.type);
        for (int side = 0; side < 4; side++)
            if ((mask >> side & 1) == 1) view.segments[side] = MakeSegment(view.content, side);

        // у хреста нитки не з'єднуються — кружечка в центрі немає
        if (tile.type != ThreadTileType.Empty && tile.type != ThreadTileType.Cross)
        {
            RectTransform knob = NewRect("Knob", view.content);
            knob.sizeDelta = new Vector2(threadWidth * 1.6f, threadWidth * 1.6f);
            view.knob = knob.gameObject.AddComponent<Image>();
            view.knob.raycastTarget = false;
        }

        ApplyRotation(view, rotations[index]);
        return view;
    }

    // Смужка від центру до сторони
    private Image MakeSegment(RectTransform parent, int side)
    {
        float half = (cellSize - 4f) * 0.5f;
        RectTransform seg = NewRect("Side_" + (ThreadSide)side, parent);
        bool vertical = side == 0 || side == 2;
        seg.sizeDelta = vertical ? new Vector2(threadWidth, half) : new Vector2(half, threadWidth);
        Vector2Int step = ThreadPuzzleSolver.Step(side);
        seg.anchoredPosition = new Vector2(step.x, -step.y) * (half * 0.5f);   // у UI y вгору, у сітці — вниз

        var img = seg.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    // Голка або котушка біля краю клітинки, за межами сітки
    private Image MakeMarker(string name, Vector2Int cell, int side, Vector2 size, Color color)
    {
        RectTransform rt = NewRect(name, board);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        Vector2Int step = ThreadPuzzleSolver.Step(side);
        rt.anchoredPosition = CellCenter(cell.x, cell.y) + new Vector2(step.x, -step.y) * (cellSize * 0.5f + 26f);
        bool vertical = side == 0 || side == 2;
        rt.sizeDelta = vertical ? size : new Vector2(size.y, size.x);

        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Vector2 CellCenter(int x, int y) => new Vector2((x + 0.5f) * cellSize, -(y + 0.5f) * cellSize);

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    // ---------- Гра ----------

    private void OnTileClicked(int index)
    {
        if (busy) return;

        rotations[index] = (rotations[index] + 1) % 4;
        ApplyRotation(tiles[index], rotations[index]);
        AudioManager.PlaySfx(rotateSound);

        if (Refresh()) StartCoroutine(SolvedRoutine());
    }

    // Поворот за годинниковою стрілкою = мінус по осі z в UI
    private static void ApplyRotation(TileView view, int rotation)
    {
        view.content.localEulerAngles = new Vector3(0f, 0f, -90f * rotation);
    }

    // Перефарбувати нитки за результатом пошуку. Повертає true, якщо розв'язано
    private bool Refresh()
    {
        ThreadPuzzleSolver.Result result = ThreadPuzzleSolver.Solve(data, rotations);

        foreach (TileView view in tiles)
        {
            foreach (Image seg in view.segments) if (seg != null) seg.color = darkThread;
            if (view.knob != null) view.knob.color = darkThread;
        }

        for (int line = 0; line < data.Lines.Length; line++)
        {
            Color c = ColorOf(data.Lines[line].color);
            foreach (int key in result.lit[line])
            {
                int index = key / 16;
                int channel = key % 16;
                TileView view = tiles[index];

                for (int baseSide = 0; baseSide < 4; baseSide++)
                {
                    if (view.segments[baseSide] == null) continue;
                    int realSide = (baseSide + rotations[index]) % 4;    // куди ця смужка дивиться після повороту
                    if ((channel >> realSide & 1) == 1) view.segments[baseSide].color = c;
                }
                if (view.knob != null) view.knob.color = c;
            }

            spools[line].color = result.reachedSpool[line] ? c : Dim(c);   // котушка яскравішає, коли нитка дійшла
        }
        return result.solved;
    }

    // Нитки падають, потім вікно закривається і блокер зникає
    private IEnumerator SolvedRoutine()
    {
        busy = true;
        AudioManager.PlaySfx(solvedSound);
        yield return new WaitForSeconds(0.4f);

        var falling = new List<Image>();
        foreach (TileView view in tiles)
        {
            foreach (Image seg in view.segments) if (seg != null) falling.Add(seg);
            if (view.knob != null) falling.Add(view.knob);
        }

        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            foreach (Image img in falling)
            {
                img.rectTransform.position += Vector3.down * (600f * Time.deltaTime);
                Color c = img.color;
                c.a = 1f - t / fallTime;
                img.color = c;
            }
            yield return null;
        }

        busy = false;
        rotations = null;       // наступне відкриття побудує пазл заново
        Close();
        onSolved?.Invoke();
    }

    // Підказка Марки, рівень 3 (задача 27): підсвітити плитку, яка повернута не так.
    // Повертає false, якщо всі плитки шляху вже стоять правильно
    public bool HighlightWrongTile()
    {
        if (!IsOpen || busy) return false;
        int index = ThreadPuzzleSolver.FindWrongTile(data, rotations);
        if (index < 0) return false;
        StartCoroutine(Flash(tiles[index].background));
        return true;
    }

    private IEnumerator Flash(Image background)
    {
        for (int i = 0; i < 3; i++)
        {
            background.color = hintColor;
            yield return new WaitForSeconds(0.25f);
            background.color = tileColor;
            yield return new WaitForSeconds(0.25f);
        }
    }

    private Color ColorOf(ThreadColor color) => color == ThreadColor.Red ? redThread : whiteThread;

    // Притемнений колір (прозорість лишається 1)
    private static Color Dim(Color c) => Color.Lerp(c, Color.black, 0.45f);
}
