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
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        HideAll();
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
