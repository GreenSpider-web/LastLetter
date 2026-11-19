using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Вікно «читання над парою» в ігровій сцені (задача 16).
// Відкриває його SteamKettle, коли Ліля несе лист до чайника.
// Усередині — та сама міні-гра, що в Sandbox_Steam: SteamMinigame + SteamReader.
// Конверт відкрито — звук паперу, печатка зламана, Втручання +1 (це робить SteamReader), потім репліка Марки.
// ПКМ — передумати й закрити вікно, не читаючи.
public class UI_Letter : MonoBehaviour
{
    [SerializeField] private GameObject window;         // затемнення, чайник, пара, конверт
    [SerializeField] private SteamMinigame minigame;
    [SerializeField] private SteamReader reader;

    [Header("Звук")]
    [SerializeField] private AudioClip steamLoop;       // sfx_steam — поки вікно відкрите
    [SerializeField] private AudioClip paperSound;      // sfx_paper_letter — конверт відкрився

    [Tooltip("Скільки секунд показувати розпечатаний конверт перед закриттям")]
    [SerializeField] private float closeDelay = 1.2f;

    [Tooltip("Вузол Ink після читання")]
    [SerializeField] private string afterReadingKnot = "marka_after_reading";

    private PlayerController player;
    private AudioSource loop;

    public bool IsOpen => window.activeSelf;

    private void Awake()
    {
        window.SetActive(false);
        minigame.onOpened.AddListener(OnEnvelopeOpened);
    }

    public void Open(LetterData letter)
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player != null) { player.Stop(); player.CanMove = false; }

        reader.SetLetter(letter);
        window.SetActive(true);
        minigame.ResetGame();
        loop = AudioManager.PlayLoop(steamLoop);
    }

    private void Update()
    {
        if (!IsOpen || PauseMenu.IsPaused) return;

        // ПКМ — передумала читати
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            StopAllCoroutines();
            Close(false);
        }
    }

    private void OnEnvelopeOpened()
    {
        AudioManager.StopLoop(loop);
        loop = null;
        AudioManager.PlaySfx(paperSound);
        StartCoroutine(CloseLater());
    }

    private IEnumerator CloseLater()
    {
        yield return new WaitForSeconds(closeDelay);
        Close(true);
    }

    private void Close(bool wasRead)
    {
        AudioManager.StopLoop(loop);
        loop = null;
        window.SetActive(false);
        if (player != null) player.CanMove = true;

        if (wasRead && DialogueManager.Instance != null && !string.IsNullOrEmpty(afterReadingKnot))
            DialogueManager.Instance.StartKnot(afterReadingKnot);
    }
}
