using UnityEngine;

// Яка музика грає в цій сцені. Один об'єкт на сцену.
// MainMenu — mus_main_theme; ділянки глави 1 — mus_mezha_ambient (коли з'явиться).
// Порожнє поле — у сцені тиша (наприклад, щоб музика меню не грала в грі).
public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;

    private void Start()
    {
        AudioManager.PlayMusic(music);
    }
}
