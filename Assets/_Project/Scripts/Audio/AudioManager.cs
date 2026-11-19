using UnityEngine;

// Звук гри (задача 32, мінімальна версія): одна доріжка музики і короткі звуки (SFX).
// Створюється сам при першому виклику, як SceneLoader, і живе до кінця гри.
// Гучність береться з налаштувань (SettingsMenu): загальна — AudioListener, музика і звуки — тут.
// Самі файли звуків вказуються в Inspector у тих скриптах, які їх грають.
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    private AudioSource music;      // музика або ембієнт, зациклені
    private AudioSource sfx;        // короткі звуки поверх музики

    // Яка музика грає зараз (null — тиша)
    public static AudioClip CurrentMusic => instance != null ? instance.music.clip : null;

    private static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("AudioManager");
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
    }

    // Повзунок «Музика» в налаштуваннях діє одразу
    private void Update()
    {
        music.volume = SettingsMenu.MusicVolume;
    }

    // Увімкнути музику. Та сама вже грає — не починати спочатку. null — тиша
    public static void PlayMusic(AudioClip clip)
    {
        if (clip == null) { StopMusic(); return; }

        AudioSource m = Instance.music;
        if (m.clip == clip && m.isPlaying) return;
        m.clip = clip;
        m.volume = SettingsMenu.MusicVolume;
        m.Play();
    }

    public static void StopMusic()
    {
        if (instance == null) return;
        instance.music.Stop();
        instance.music.clip = null;
    }

    // Короткий звук: клік, папір, крок
    public static void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        Instance.sfx.PlayOneShot(clip, SettingsMenu.SfxVolume);
    }

    // Зациклений звук на окремому джерелі (пара над чайником). Зупинити — Stop() у того, хто викликав
    public static AudioSource PlayLoop(AudioClip clip)
    {
        if (clip == null) return null;
        AudioSource loop = Instance.gameObject.AddComponent<AudioSource>();
        loop.clip = clip;
        loop.loop = true;
        loop.volume = SettingsMenu.SfxVolume;
        loop.Play();
        return loop;
    }

    // Зупинити й прибрати зациклений звук
    public static void StopLoop(AudioSource loop)
    {
        if (loop == null) return;
        loop.Stop();
        Destroy(loop);
    }
}
