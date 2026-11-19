using UnityEngine;
using UnityEngine.Events;

// Блокер із ниток у сцені (хвіртка на 1-Б, прохід на 1-В): клік — відкривається пазл.
// Розв'язали — ставиться прапорець із ThreadPuzzleData, блокер зникає, викликається onSolved
// (у сцені через нього можна, наприклад, увімкнути прохід далі).
[RequireComponent(typeof(Interactable))]
public class ThreadPuzzleTrigger : MonoBehaviour
{
    [SerializeField] private ThreadPuzzleData data;
    [SerializeField] private ThreadPuzzleUI puzzleUI;

    [Tooltip("Що зникає після розв'язку. Порожньо — сам цей об'єкт")]
    [SerializeField] private GameObject blocker;

    public UnityEvent onSolved;

    public ThreadPuzzleData Data => data;
    public bool IsSolved => GameState.Instance != null && GameState.Instance.GetFlag(data.SolvedFlag);

    private void Awake()
    {
        if (blocker == null) blocker = gameObject;
        GetComponent<Interactable>().onInteract.AddListener(Open);
    }

    // Після «Продовжити» розв'язаний пазл не повертається
    private void Start()
    {
        if (IsSolved) blocker.SetActive(false);
    }

    public void Open()
    {
        if (puzzleUI == null) puzzleUI = FindAnyObjectByType<ThreadPuzzleUI>();
        if (puzzleUI == null) { Debug.LogError("ThreadPuzzleTrigger: у сцені немає UI_ThreadPuzzle"); return; }
        puzzleUI.Open(data, OnSolved);
    }

    private void OnSolved()
    {
        if (GameState.Instance != null && !string.IsNullOrEmpty(data.SolvedFlag))
            GameState.Instance.SetFlag(data.SolvedFlag, true);

        onSolved?.Invoke();
        blocker.SetActive(false);
    }
}
