using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Автозбереження в один слот (JSON у Application.persistentDataPath/save.json).
// Зберігає, коли Ліля заходить у ділянку, змінюється прапорець / Втручання / сумка, і при виході з гри.
// «Продовжити» — відновлює стан і завантажує ту саму ділянку; «Нова гра» — стирає збереження.
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [SerializeField] private string menuScene = "MainMenu";
    [Tooltip("Перша сцена нової гри. Коли буде задача 17 — L1_PostSquare")]
    [SerializeField] private string firstScene = "Sandbox_Player";

    private bool dirty;         // щось змінилось — зберегти в кінці кадру
    private bool applying;      // зараз відновлюємо збереження — не зберігати посередині

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool HasSave => File.Exists(SavePath);

    private bool InGame => SceneManager.GetActiveScene().name != menuScene;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // GameState і InventoryManager теж живуть між сценами, тож підписуємось один раз
        if (GameState.Instance != null)
        {
            GameState.Instance.OnFlagChanged += (name, value) => MarkDirty();
            GameState.Instance.OnInterventionChanged += value => MarkDirty();
        }
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnChanged += MarkDirty;
    }

    private void MarkDirty()
    {
        if (!applying) dirty = true;
    }

    // Кілька змін за кадр (напр. поєднання: −2 предмети, +1) — одне збереження
    private void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        Save();
    }

    private void OnApplicationQuit() => Save();

    public void Save()
    {
        if (!InGame || GameState.Instance == null || InventoryManager.Instance == null) return;

        var data = new SaveData { scene = SceneManager.GetActiveScene().name };

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
        }

        foreach (string flag in GameState.Instance.AllFlags)
            data.flags.Add(new FlagEntry { name = flag, value = GameState.Instance.GetFlag(flag) });
        data.intervention = GameState.Instance.Intervention;

        foreach (ItemData item in InventoryManager.Instance.Items) data.items.Add(item.id);
        data.pickedUp.AddRange(InventoryManager.Instance.PickedUp);

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Автозбереження: " + data.scene);
    }

    // Кнопка «Продовжити»
    public void Continue()
    {
        if (!HasSave) return;
        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

        applying = true;
        foreach (FlagEntry f in data.flags) GameState.Instance.SetFlag(f.name, f.value);
        GameState.Instance.SetIntervention(data.intervention);
        InventoryManager.Instance.LoadState(data.items, data.pickedUp);
        applying = false;

        SceneLoader.Load(data.scene, new Vector2(data.playerX, data.playerY));
    }

    // Кнопка «Нова гра»
    public void NewGame()
    {
        if (HasSave) File.Delete(SavePath);

        applying = true;
        GameState.Instance.ResetAll();
        InventoryManager.Instance.Clear();
        applying = false;

        SceneLoader.Load(firstScene);
    }
}
