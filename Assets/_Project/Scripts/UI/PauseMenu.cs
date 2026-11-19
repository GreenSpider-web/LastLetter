using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Пауза в ігрових сценах: Esc — зупинити гру (Time.timeScale = 0).
// Продовжити, Як грати, Налаштування, Головне меню (зі збереженням), Вихід.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string menuScene = "MainMenu";

    [Header("Музика")]                                                                  // [NEW]
    [Tooltip("На паузі грає та сама тема, що в головному меню (mus_main_theme)")]      // [NEW]
    [SerializeField] private AudioClip pauseMusic;                                      // [NEW]
    private AudioClip musicBeforePause;                                                 // [NEW] що грало в сцені до паузи

    public static bool IsPaused { get; private set; }

    private void Start()
    {
        resumeButton.onClick.AddListener(Resume);
        howToPlayButton.onClick.AddListener(() => howToPlayPanel.SetActive(true));
        settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
        menuButton.onClick.AddListener(ToMainMenu);
        quitButton.onClick.AddListener(QuitGame);

        HideAll();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // Esc: спершу закрити вкладене вікно, потім паузу; якщо паузи немає — поставити
        if (howToPlayPanel.activeSelf || settingsPanel.activeSelf)
        {
            howToPlayPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }
        else if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        musicBeforePause = AudioManager.CurrentMusic;   // [NEW]
        AudioManager.PlayMusic(pauseMusic);             // [NEW]
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        HideAll();
        AudioManager.PlayMusic(musicBeforePause);   // [NEW] музика сцени (null — тиша)
    }

    private void ToMainMenu()
    {
        if (SaveSystem.Instance != null) SaveSystem.Instance.Save();
        Resume();
        SceneLoader.Load(menuScene);
    }

    private void QuitGame()
    {
        if (SaveSystem.Instance != null) SaveSystem.Instance.Save();
        MainMenu.Quit();
    }

    private void HideAll()
    {
        pausePanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    // Вийшли зі сцени під час паузи — не лишити гру «замороженою»
    private void OnDestroy()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
