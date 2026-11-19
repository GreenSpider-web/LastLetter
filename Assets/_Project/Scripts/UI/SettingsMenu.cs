using UnityEngine;
using UnityEngine.UI;

// Вікно налаштувань: гучність (загальна, музика, звуки) і повний екран.
// Налаштування — не частина сейву гри: вони в PlayerPrefs і спільні для всіх проходжень.
public class SettingsMenu : MonoBehaviour
{
    private const string MasterKey = "volume_master";
    private const string MusicKey = "volume_music";
    private const string SfxKey = "volume_sfx";
    private const string FullscreenKey = "fullscreen";

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle fullscreenToggle;

    // AudioManager (задача 32) бере звідси гучність музики й звуків
    public static float MusicVolume => PlayerPrefs.GetFloat(MusicKey, 0.8f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxKey, 1f);

    // Застосувати збережені налаштування одразу при запуску гри
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnStart()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterKey, 1f);
        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
    }

    private void Awake()
    {
        masterSlider.onValueChanged.AddListener(OnMaster);
        musicSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(MusicKey, v));
        sfxSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SfxKey, v));
        fullscreenToggle.onValueChanged.AddListener(OnFullscreen);
    }

    // Щоразу, коли вікно відкривають, — показати поточні значення
    private void OnEnable()
    {
        masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterKey, 1f));
        musicSlider.SetValueWithoutNotify(MusicVolume);
        sfxSlider.SetValueWithoutNotify(SfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private void OnDisable() => PlayerPrefs.Save();

    private void OnMaster(float value)
    {
        PlayerPrefs.SetFloat(MasterKey, value);
        AudioListener.volume = value;
    }

    private void OnFullscreen(bool on)
    {
        PlayerPrefs.SetInt(FullscreenKey, on ? 1 : 0);
        Screen.fullScreen = on;
    }
}
