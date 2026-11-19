using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Перехід між сценами з затемненням: 0.5 с у чорне → завантаження → 0.5 с назад.
// Створюється сам при першому виклику SceneLoader.Load(...) і живе до кінця гри.
public class SceneLoader : MonoBehaviour
{
    private const float FadeTime = 0.5f;    // GDD: затемнення 0.5 с

    private static SceneLoader instance;

    private CanvasGroup fade;
    private bool busy;

    // spawn — куди поставити Лілю (для «Продовжити»); без нього — як у сцені
    public static void Load(string scene, Vector2? spawn = null)
    {
        if (instance == null)
        {
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
            instance.CreateFade();
        }
        if (!instance.busy) instance.StartCoroutine(instance.LoadRoutine(scene, spawn));
    }

    private IEnumerator LoadRoutine(string scene, Vector2? spawn)
    {
        busy = true;
        fade.blocksRaycasts = true;     // поки темно — кліки не проходять
        Time.timeScale = 1f;            // якщо виходили в меню з паузи

        yield return Fade(0f, 1f);
        yield return SceneManager.LoadSceneAsync(scene);

        if (spawn.HasValue)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.position = new Vector3(spawn.Value.x, spawn.Value.y, player.transform.position.z);
                player.Stop();
            }
        }

        if (SaveSystem.Instance != null) SaveSystem.Instance.Save();   // автозбереження при вході в ділянку

        yield return Fade(1f, 0f);
        fade.blocksRaycasts = false;
        busy = false;
    }

    // unscaledDeltaTime — щоб працювало і при Time.timeScale = 0
    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            fade.alpha = Mathf.Lerp(from, to, t / FadeTime);
            yield return null;
        }
        fade.alpha = to;
    }

    // Чорний екран поверх усього UI
    private void CreateFade()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        gameObject.AddComponent<GraphicRaycaster>();

        var black = new GameObject("Black", typeof(RectTransform));
        black.transform.SetParent(transform, false);
        var rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        black.AddComponent<Image>().color = Color.black;

        fade = gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
    }
}
