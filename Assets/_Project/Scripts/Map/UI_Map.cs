using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Вікно «Мапа Межі»: 6 районів, у кожного свій стан (MapManager).
// Відкрити — M або значок мапи в HUD (верхній правий кут). Закрити — M, ПКМ або Esc.
// Створюється саме при першому відкритті, у сцену нічого класти не треба.
// Арт — Resources/UI/map_mezha.png. Закритий район — у темному тумані; пройдений (можна йти далі) — без туману
// і світло-жовтий; район, де зараз Ліля, — без туману, без підсвітки, над ним «Ти тут».   // [CHANGED]
public class UI_Map : MonoBehaviour
{
    private static UI_Map instance;

    private GameObject window;
    private PlayerController player;
    private bool shownOnce;     // [NEW] першого разу туман не анімуємо — просто показуємо стан

    // що треба оновлювати в кожному районі при відкритті
    private class DistrictView
    {
        public Image fog;           // [CHANGED] темний туман над районом (замість fill і state)
        public Image glow;          // [NEW] світло-жовта підсвітка пройденого району
        public Text here;
        public Vector2 fogPos;      // [NEW] де стоїть туман — від цієї точки він ледь «дихає»
        public Vector2 herePos;     // [NEW] де стоїть «Ти тут» — від цієї точки погойдується
        public bool wasOpen;        // [NEW] чи був район без туману минулого разу — розвіювати лише один раз
        public Coroutine anim;      // [NEW] розвіювання, що зараз іде
    }
    private readonly Dictionary<District, DistrictView> views = new Dictionary<District, DistrictView>();

    // [CHANGED] Де район на картинці мапи, у пікселях map_mezha.png (1267×705):
    // x, y — лівий верхній кут прямокутника (0, 0 — лівий верхній кут картинки), далі ширина й висота
    private static readonly Dictionary<District, Rect> Areas = new Dictionary<District, Rect>
    {
        { District.SortingRoom,  new Rect(240,  10, 330, 200) },
        { District.PostSquare,   new Rect(140, 165, 430, 305) },
        { District.TailorStreet, new Rect( 10, 430, 400, 270) },
        { District.Bridge,       new Rect(560, 280, 230, 140) },
        { District.Trenches,     new Rect(740,   0, 300, 320) },
        { District.BridgePost,   new Rect(780, 320, 260, 170) },
    };

    // [NEW] Де писати «Ти тут» — трохи над назвою району на картинці (пікселі map_mezha.png)
    private static readonly Dictionary<District, Vector2> HerePoints = new Dictionary<District, Vector2>
    {
        { District.SortingRoom,  new Vector2(349,  22) },
        { District.PostSquare,   new Vector2(355, 290) },
        { District.TailorStreet, new Vector2(135, 477) },
        { District.Bridge,       new Vector2(680, 316) },
        { District.Trenches,     new Vector2(893,  12) },
        { District.BridgePost,   new Vector2(897, 392) },
    };

    private static readonly Color FogColor = new Color(0.08f, 0.07f, 0.06f, 0.85f);   // [CHANGED] темний туман — карту під ним ледь видно
    private static readonly Color GlowColor = new Color(1f, 0.92f, 0.55f, 0.45f);     // [CHANGED] світло-жовта підсвітка (було LightColor)
    private static readonly Color HereColor = new Color(0.6f, 0.12f, 0.1f);           // [NEW] «Ти тут»
    private static readonly Color PaperColor = new Color(0.93f, 0.87f, 0.74f);        // [CHANGED] заголовок над мапою (було Ink)

    private const float MapWidth = 1500f;       // [NEW] ширина мапи на екрані (екран 1920)
    private const float DisperseTime = 1.5f;    // [NEW] скільки триває розвіювання туману, с

    public static bool IsOpen => instance != null && instance.window.activeSelf;

    public static void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public static void Open()
    {
        if (instance == null)
        {
            instance = new GameObject("UI_Map").AddComponent<UI_Map>();
            DontDestroyOnLoad(instance.gameObject);
            instance.Build();
        }
        instance.Show();
    }

    public static void Close()
    {
        if (!IsOpen) return;
        instance.window.SetActive(false);
        instance.StartCoroutine(instance.EnableMoveNextFrame());
    }

    private void Show()
    {
        District here = MapManager.Current(SceneManager.GetActiveScene().name);
        foreach (District d in MapManager.All)
        {
            DistrictView v = views[d];
            bool isHere = d == here;
            bool passed = MapManager.GetState(d) == DistrictState.Light && !isHere;   // [CHANGED] пройдено = можна йти далі; поточний не підсвічуємо
            bool open = passed || isHere;                                             // [NEW] з туману виходять пройдені й поточний

            v.here.enabled = isHere;

            if (v.anim != null) StopCoroutine(v.anim);                                // [NEW]
            if (open && !v.wasOpen && shownOnce)
                v.anim = StartCoroutine(Disperse(v, passed));                        // [NEW] район щойно відкрився — туман розвіюється
            else
            {
                SetFog(v, open ? 0f : 1f);                                            // [NEW]
                SetGlow(v, passed ? 1f : 0f);                                         // [NEW]
            }
            v.wasOpen = open;                                                         // [NEW]
        }
        shownOnce = true;                                                             // [NEW]

        player = FindAnyObjectByType<PlayerController>();
        if (player != null) { player.Stop(); player.CanMove = false; }
        window.SetActive(true);
    }

    private void Update()
    {
        if (!IsOpen || PauseMenu.IsPaused) return;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Close();

        // [NEW] туман ледь «дихає», «Ти тут» погойдується — щоб мапа не була мертвою картинкою
        float time = Time.unscaledTime;
        foreach (DistrictView v in views.Values)
        {
            v.fog.rectTransform.anchoredPosition = v.fogPos +
                new Vector2(Mathf.Sin(time * 0.6f + v.fogPos.x), Mathf.Cos(time * 0.5f + v.fogPos.y)) * 6f;
            v.here.rectTransform.anchoredPosition = v.herePos + Vector2.up * Mathf.Sin(time * 3f) * 4f;
        }
    }

    // Той самий ПКМ не має одразу спрацювати в сцені як «оглянути»
    private IEnumerator EnableMoveNextFrame()
    {
        yield return null;
        if (player != null) player.CanMove = true;
    }

    // ---------- Туман і підсвітка ----------   // [NEW]

    // [NEW] Туман за DisperseTime тане й трохи розповзається; підсвітка пройденого району проступає
    private IEnumerator Disperse(DistrictView v, bool passed)
    {
        for (float t = 0f; t < DisperseTime; t += Time.unscaledDeltaTime)   // unscaled — мапа працює й на паузі
        {
            float k = t / DisperseTime;     // 0 → 1
            SetFog(v, 1f - k);
            SetGlow(v, passed ? k : 0f);
            yield return null;
        }
        SetFog(v, 0f);
        SetGlow(v, passed ? 1f : 0f);
        v.anim = null;
    }

    // [NEW] amount: 1 — густий туман, 0 — немає; що тонший туман, то ширше він розповзається
    private static void SetFog(DistrictView v, float amount)
    {
        Color c = FogColor;
        c.a = FogColor.a * amount;
        v.fog.color = c;
        v.fog.enabled = amount > 0.01f;
        v.fog.rectTransform.localScale = Vector3.one * (1.25f - 0.25f * amount);
    }

    // [NEW] amount: 1 — повна підсвітка, 0 — немає
    private static void SetGlow(DistrictView v, float amount)
    {
        Color c = GlowColor;
        c.a = GlowColor.a * amount;
        v.glow.color = c;
    }

    // [NEW] М'яка пляма, намальована кодом: щільна в центрі, тане до країв.
    // noise > 0 — краї рвані й усередині «клаптики», як у туману; seed — щоб у кожного району був свій візерунок
    private static Sprite SoftSprite(int seed, float noise)
    {
        return UIBuild.DrawSprite(128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);                                        // 0 — центр, 1 — край
            float n = Mathf.PerlinNoise(x * 2.5f + seed * 10f, y * 2.5f + seed * 7f);   // шум 0..1
            float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.45f, 1f, r + (n - 0.5f) * noise));
            a *= 1f - noise * 0.4f * (1f - n);
            return new Color(1f, 1f, 1f, a);
        });
    }

    // ---------- Вікно: затемнення, аркуш, 6 районів ----------

    private void Build()
    {
        UIBuild.MakeCanvas(gameObject, 25);   // над діалогом (20), під паузою (50)

        window = UIBuild.Rect("Window", transform).gameObject;
        UIBuild.Stretch((RectTransform)window.transform);
        window.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);   // ще й не пускає кліки в сцену

        // [CHANGED] замість паперу-заглушки — картинка мапи; висоту рахуємо з картинки, щоб не сплющилась
        Sprite mapSprite = Resources.Load<Sprite>("UI/map_mezha");                                 // [NEW] Assets/_Project/Resources/UI/map_mezha.png
        if (mapSprite == null) Debug.LogError("UI_Map: немає картинки Resources/UI/map_mezha.png"); // [NEW]
        Vector2 picSize = mapSprite != null ? mapSprite.rect.size : new Vector2(1267f, 705f);       // [NEW] розмір картинки в пікселях
        float scale = MapWidth / picSize.x;                                                         // [NEW] пікселі картинки → одиниці екрана
        Image map = UIBuild.Panel("Map", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f),
                                  picSize * scale, Color.white);                                    // [CHANGED]
        map.sprite = mapSprite;                                                                     // [NEW]

        Text title = UIBuild.Label("Title", window.transform, 48, FontStyle.Bold, TextAnchor.UpperCenter, PaperColor);   // [CHANGED] над мапою
        title.text = "Мапа Межі";
        title.rectTransform.offsetMax = new Vector2(0f, -45f);   // [CHANGED]

        Text hint = UIBuild.Label("Hint", window.transform, 24, FontStyle.Normal, TextAnchor.LowerCenter, new Color(0.75f, 0.7f, 0.6f));   // [CHANGED] під мапою
        hint.text = "M або ПКМ — закрити";
        hint.rectTransform.offsetMin = new Vector2(0f, 35f);     // [CHANGED]

        foreach (District d in MapManager.All)
            views[d] = BuildDistrict(map.transform, d, scale, picSize);   // [CHANGED]

        // [NEW] порядок малювання: спершу всі підсвітки, над ними туман, зверху «Ти тут»
        foreach (DistrictView v in views.Values) v.fog.transform.SetAsLastSibling();
        foreach (DistrictView v in views.Values) v.here.transform.SetAsLastSibling();

        window.SetActive(false);
    }

    // [CHANGED] Район = підсвітка + туман над його прямокутником на картинці + напис «Ти тут» над назвою
    private DistrictView BuildDistrict(Transform map, District d, float scale, Vector2 picSize)
    {
        Rect area = Areas[d];
        Vector2 center = ToMap(area.center, scale, picSize);
        Vector2 size = area.size * scale;

        Image glow = UIBuild.Panel(d + "_Glow", map, new Vector2(0.5f, 0.5f), center, size * 1.15f, GlowColor);
        glow.sprite = SoftSprite(0, 0f);            // рівна м'яка пляма
        glow.raycastTarget = false;

        Image fog = UIBuild.Panel(d + "_Fog", map, new Vector2(0.5f, 0.5f), center, size * 1.4f, FogColor);
        fog.sprite = SoftSprite((int)d + 3, 0.35f); // рваний туман, у кожного району свій
        fog.raycastTarget = false;

        Vector2 herePos = ToMap(HerePoints[d], scale, picSize);
        Text here = UIBuild.Label("Here_" + d, map, 30, FontStyle.Bold, TextAnchor.MiddleCenter, HereColor);
        here.text = "Ти тут";
        here.rectTransform.anchorMin = here.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        here.rectTransform.sizeDelta = new Vector2(200f, 40f);
        here.rectTransform.anchoredPosition = herePos;
        here.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.94f, 0.84f, 0.9f);   // світла обводка — читається і на темному

        return new DistrictView { fog = fog, glow = glow, here = here, fogPos = center, herePos = herePos };
    }

    // [NEW] Точка на картинці (пікселі, 0, 0 — лівий верхній кут) → позиція на мапі (0, 0 — центр, y угору)
    private static Vector2 ToMap(Vector2 pixel, float scale, Vector2 picSize)
    {
        return new Vector2(pixel.x - picSize.x / 2f, picSize.y / 2f - pixel.y) * scale;
    }
}
