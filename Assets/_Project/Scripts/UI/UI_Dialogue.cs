using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Вікно діалогу внизу екрана: ім'я, портрет, текст, що друкується по літері, варіанти відповіді.
// Клік по панелі / Space / Enter: перший раз — дописати рядок, другий — наступна репліка.
// Що показувати, вирішує DialogueManager; це лише «екран».
public class UI_Dialogue : MonoBehaviour
{
    [SerializeField] private GameObject window;           // панель + портрет + варіанти
    [SerializeField] private Button panelButton;          // клік по панелі — далі
    [SerializeField] private Text nameText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Image portrait;
    [SerializeField] private Button[] choiceButtons;      // 4 кнопки (у GDD — до 4 варіантів)
    [Tooltip("Швидкість друку, літер за секунду")]
    [SerializeField] private float charsPerSecond = 45f;

    private string fullText = "";
    private float shownChars;
    private List<string> pendingChoices;   // варіанти чекають, доки допишеться рядок
    private bool choicesShown;

    private bool IsTyping => shownChars < fullText.Length;

    private void Awake()
    {
        panelButton.onClick.AddListener(Next);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;      // копія для лямбди
            choiceButtons[i].onClick.AddListener(() => DialogueManager.Instance.Choose(index));
        }
        Hide();
    }

    private void Update()
    {
        if (!window.activeSelf) return;

        if (IsTyping)
        {
            shownChars += charsPerSecond * Time.deltaTime;
            bodyText.text = fullText.Substring(0, Mathf.Min(fullText.Length, (int)shownChars));
            if (!IsTyping) ShowPendingChoices();
        }

        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) Next();
    }

    public void ShowLine(string speaker, string text, Sprite face, Color nameColor, bool narrator)
    {
        window.SetActive(true);

        nameText.text = speaker;
        nameText.color = nameColor;
        bodyText.fontStyle = narrator ? FontStyle.Italic : FontStyle.Normal;

        portrait.sprite = face;
        portrait.enabled = face != null;

        fullText = text;
        shownChars = 0f;
        bodyText.text = "";
        pendingChoices = null;
        HideChoices();
    }

    public void ShowChoices(List<string> texts)
    {
        if (texts.Count > choiceButtons.Length)
            Debug.LogWarning($"UI_Dialogue: варіантів {texts.Count}, а кнопок {choiceButtons.Length}");

        pendingChoices = texts;
        if (!IsTyping) ShowPendingChoices();
    }

    public void Hide()
    {
        window.SetActive(false);
        HideChoices();
    }

    private void Next()
    {
        if (IsTyping)
        {
            // перший клік — одразу дописати рядок
            shownChars = fullText.Length;
            bodyText.text = fullText;
            ShowPendingChoices();
            return;
        }
        if (choicesShown) return;       // поки є варіанти — треба вибрати один
        DialogueManager.Instance.Continue();
    }

    private void ShowPendingChoices()
    {
        if (pendingChoices == null) return;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool has = i < pendingChoices.Count;
            choiceButtons[i].gameObject.SetActive(has);
            if (has) choiceButtons[i].GetComponentInChildren<Text>().text = pendingChoices[i];
        }
        pendingChoices = null;
        choicesShown = true;
    }

    private void HideChoices()
    {
        foreach (Button b in choiceButtons) b.gameObject.SetActive(false);
        choicesShown = false;
    }
}
