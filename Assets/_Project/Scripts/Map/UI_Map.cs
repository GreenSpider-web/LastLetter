using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Вікно «Мапа Межі»: 6 районів, у кожного свій стан (MapManager).
// Відкрити — M або значок мапи в HUD (верхній правий кут). Закрити — M, ПКМ або Esc.
// Створюється саме при першому відкритті, у сцену нічого класти не треба. Арт мапи — заглушка.
public class UI_Map : MonoBehaviour
{
    private static UI_Map instance;

    private GameObject window;
    private PlayerController player;

    // що треба оновлювати в кожному районі при відкритті
    private class DistrictView
    {
        public Image fill;
        public Text state;
        public Text here;
    }
    private readonly Dictionary<District, DistrictView> views = new Dictionary<District, DistrictView>();

    // Де стоїть район на аркуші мапи (центр аркуша = 0, 0)
    private static readonly Dictionary<District, Vector2> Positions = new Dictionary<District, Vector2>
    {
        { District.TailorStreet, new Vector2(-300f,  90f) },
        { District.SortingRoom,  new Vector2(   0f, 120f) },
        { District.Trenches,     new Vector2( 300f, 150f) },
        { District.PostSquare,   new Vector2(-280f, -140f) },
        { District.Bridge,       new Vector2(  40f, -170f) },
        { District.BridgePost,   new Vector2( 320f, -100f) },
    };

    private static readonly Color FogColor = new Color(0.55f, 0.52f, 0.45f, 0.95f);
    private static readonly Color LightColor = new Color(0.98f, 0.82f, 0.45f, 1f);
    private static readonly Color Ink = new Color(0.25f, 0.18f, 0.12f);

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
            DistrictState s = MapManager.GetState(d);
            DistrictView v = views[d];
            v.fill.color = s == DistrictState.Light ? LightColor : s == DistrictState.Fog ? FogColor : Color.clear;
            v.state.text = MapManager.StateName(s);
            v.here.enabled = d == here;
        }

        player = FindAnyObjectByType<PlayerController>();
        if (player != null) { player.Stop(); player.CanMove = false; }
        window.SetActive(true);
    }

    private void Update()
    {
        if (!IsOpen || PauseMenu.IsPaused) return;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Close();
    }

    // Той самий ПКМ не має одразу спрацювати в сцені як «оглянути»
    private IEnumerator EnableMoveNextFrame()
    {
        yield return null;
        if (player != null) player.CanMove = true;
    }

    // ---------- Вікно: затемнення, аркуш, 6 районів ----------

    private void Build()
    {
        UIBuild.MakeCanvas(gameObject, 25);   // над діалогом (20), під паузою (50)

        window = UIBuild.Rect("Window", transform).gameObject;
        UIBuild.Stretch((RectTransform)window.transform);
        window.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);   // ще й не пускає кліки в сцену

        Image paper = UIBuild.Panel("Paper", window.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                                    new Vector2(1100f, 700f), new Color(0.9f, 0.84f, 0.7f));

        Text title = UIBuild.Label("Title", paper.transform, 44, FontStyle.Bold, TextAnchor.UpperCenter, Ink);
        title.text = "Мапа Межі";
        title.rectTransform.offsetMax = new Vector2(0f, -25f);

        Text hint = UIBuild.Label("Hint", paper.transform, 22, FontStyle.Normal, TextAnchor.LowerCenter, new Color(0.45f, 0.4f, 0.35f));
        hint.text = "M або ПКМ — закрити";
        hint.rectTransform.offsetMin = new Vector2(0f, 18f);

        foreach (District d in MapManager.All)
            views[d] = BuildDistrict(paper.transform, d);

        window.SetActive(false);
    }

    private DistrictView BuildDistrict(Transform paper, District d)
    {
        Image fill = UIBuild.Panel(d.ToString(), paper, new Vector2(0.5f, 0.5f), Positions[d], new Vector2(250f, 120f), FogColor);
        UIBuild.Frame(fill.transform, 3f, Ink);   // контур видно завжди; у «Зникло» лишається тільки він

        Text nameText = UIBuild.Label("Name", fill.transform, 28, FontStyle.Bold, TextAnchor.UpperCenter, Ink);
        nameText.text = MapManager.Name(d);
        nameText.rectTransform.offsetMax = new Vector2(0f, -18f);

        Text state = UIBuild.Label("State", fill.transform, 24, FontStyle.Italic, TextAnchor.LowerCenter, Ink);
        state.rectTransform.offsetMin = new Vector2(0f, 18f);

        Text here = UIBuild.Label("Here", fill.transform, 22, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.6f, 0.12f, 0.1f));
        here.text = "Ти тут";
        here.rectTransform.offsetMax = new Vector2(0f, 32f);   // над прямокутником району

        return new DistrictView { fill = fill, state = state, here = here };
    }
}
