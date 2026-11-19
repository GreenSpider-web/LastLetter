using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Головне меню (сцена MainMenu): Продовжити, Нова гра, Як грати, Налаштування, Вихід.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        continueButton.interactable = SaveSystem.HasSave;     // немає збереження — «Продовжити» сіра

        continueButton.onClick.AddListener(() => SaveSystem.Instance.Continue());
        newGameButton.onClick.AddListener(() => SaveSystem.Instance.NewGame());
        howToPlayButton.onClick.AddListener(() => howToPlayPanel.SetActive(true));
        settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
        quitButton.onClick.AddListener(Quit);

        howToPlayPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    // Esc закриває відкрите вікно
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        howToPlayPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
