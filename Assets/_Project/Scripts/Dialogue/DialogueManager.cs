using System;
using System.Collections.Generic;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;

// Запускає розмови з LastLetter_Ch1.ink і зв'язує змінні Ink з GameState:
//  перед розмовою GameState → Ink (Ink бачить актуальні прапорці),
//  коли Ink виконує «~ prologue_done = true» → значення одразу йде в GameState.
// Теги в Ink: #speaker:marka — хто говорить; #emotion:angry — вираз обличчя на портреті;
// #give_item:item_thimble — предмет у сумку; #sfx:… — звук (задача 32).
public class DialogueManager : MonoBehaviour
{
    [Serializable]
    public class Speaker
    {
        public string id = "marka";             // як у тегу #speaker:marka
        public string displayName = "Марка";    // що бачить гравець
        public Sprite portrait;                 // портрет 512×512 (задача 34) — звичайний вираз
        public Color nameColor = Color.white;
        [Tooltip("Інші вирази обличчя; name — як у тегу #emotion:angry")]
        public List<Emotion> emotions = new List<Emotion>();

        // Портрет для виразу обличчя; якщо такого не намальовано — звичайний
        public Sprite GetPortrait(string emotion)
        {
            Emotion e = emotions.Find(x => x.name == emotion);
            return e != null && e.portrait != null ? e.portrait : portrait;
        }
    }

    [Serializable]
    public class Emotion
    {
        public string name = "angry";           // smile, angry, surprised, scared, sad, confused
        public Sprite portrait;
    }

    public static DialogueManager Instance { get; private set; }

    [SerializeField] private InkFile inkFile;                       // LastLetter_Ch1.ink
    [SerializeField] private UI_Dialogue view;
    [SerializeField] private List<Speaker> speakers = new List<Speaker>();
    [Tooltip("Усі предмети, які Ink може видати тегом #give_item")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    [Tooltip("Звуки для тегу #sfx:назва — шукаються за назвою файлу, напр. sfx_door_slam")]   // [NEW]
    [SerializeField] private List<AudioClip> sounds = new List<AudioClip>();                  // [NEW]

    private Story story;
    private PlayerController player;
    private string currentKnot;
    private string currentEmotion;      // з тегу #emotion поточного рядка

    public bool IsOpen { get; private set; }

    // Розмова закінчилась: назва вузла (напр. "panteleimon_letter")
    public event Action<string> OnDialogueEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (view == null) view = FindAnyObjectByType<UI_Dialogue>();
        player = FindAnyObjectByType<PlayerController>();

        if (inkFile == null || !inkFile.isCompiled)
        {
            Debug.LogError("DialogueManager: не вказано .ink файл або він не скомпілювався");
            enabled = false;
            return;
        }

        story = new Story(inkFile.storyJson);
        ObserveGameState();
    }

    // Ink → GameState: коли в Ink змінюється змінна з такою самою назвою, як прапорець
    private void ObserveGameState()
    {
        GameState gs = GameState.Instance;
        if (gs == null) { Debug.LogWarning("DialogueManager: у сцені немає GameState"); return; }

        foreach (string flag in gs.AllFlags)
        {
            if (!story.variablesState.GlobalVariableExistsWithName(flag)) continue;
            story.ObserveVariable(flag, (name, value) => gs.SetFlag(name, (bool)value));
        }

        if (story.variablesState.GlobalVariableExistsWithName("intervention"))
            story.ObserveVariable("intervention", (name, value) => gs.SetIntervention((int)value));
    }

    // GameState → Ink: перед кожною розмовою, бо прапорці могли змінитися поза діалогом
    private void PushGameState()
    {
        GameState gs = GameState.Instance;
        if (gs == null) return;

        foreach (string flag in gs.AllFlags)
            if (story.variablesState.GlobalVariableExistsWithName(flag))
                story.variablesState[flag] = gs.GetFlag(flag);

        if (story.variablesState.GlobalVariableExistsWithName("intervention"))
            story.variablesState["intervention"] = gs.Intervention;
    }

    // Почати розмову: вузол Ink, напр. "panteleimon" або "hint_lantern.l2"
    public void StartKnot(string knot)
    {
        if (story == null || IsOpen) return;

        PushGameState();
        try
        {
            story.ChoosePathString(knot);
        }
        catch (StoryException e)
        {
            Debug.LogError($"DialogueManager: у Ink немає вузла «{knot}». {e.Message}");
            return;
        }

        currentKnot = knot;
        IsOpen = true;
        if (player != null) { player.Stop(); player.CanMove = false; }
        Continue();
    }

    // Наступна репліка (викликає UI_Dialogue, коли гравець клікнув «далі»)
    public void Continue()
    {
        if (!IsOpen) return;

        while (story.canContinue)
        {
            string text = story.Continue().Trim();
            string speakerId = ReadTags(story.currentTags);
            if (text.Length == 0) continue;     // рядок лише з логікою, без тексту

            ShowLine(speakerId, text);
            if (!story.canContinue && story.currentChoices.Count > 0) ShowChoices();
            return;
        }

        if (story.currentChoices.Count > 0) { ShowChoices(); return; }
        End();
    }

    // Гравець вибрав варіант відповіді
    public void Choose(int index)
    {
        if (!IsOpen || index >= story.currentChoices.Count) return;
        story.ChooseChoiceIndex(index);
        Continue();
    }

    private void ShowLine(string speakerId, string text)
    {
        Speaker s = FindSpeaker(speakerId);
        string name = s != null ? s.displayName : "";
        Sprite face = s != null ? s.GetPortrait(currentEmotion) : null;
        Color color = s != null ? s.nameColor : Color.white;
        bool narrator = speakerId == "narrator";     // опис дії — курсивом, без імені

        view.ShowLine(name, text, face, color, narrator);
    }

    private void ShowChoices()
    {
        var texts = new List<string>();
        foreach (Choice c in story.currentChoices) texts.Add(c.text);
        view.ShowChoices(texts);
    }

    private void End()
    {
        IsOpen = false;
        view.Hide();
        if (player != null) player.CanMove = true;
        OnDialogueEnded?.Invoke(currentKnot);
    }

    // Розбирає теги рядка; повертає id мовця
    private string ReadTags(List<string> tags)
    {
        string speakerId = "";
        currentEmotion = "";
        foreach (string tag in tags)
        {
            string[] parts = tag.Split(':');
            if (parts.Length < 2) continue;

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            if (key == "speaker") speakerId = value;
            else if (key == "emotion") currentEmotion = value;
            else if (key == "give_item") GiveItem(value);
            else if (key == "sfx") PlaySound(value);   // [CHANGED]
        }
        return speakerId;
    }

    private void GiveItem(string id)
    {
        ItemData item = items.Find(i => i != null && i.id == id);
        if (item == null) { Debug.LogWarning($"DialogueManager: немає ItemData з id «{id}»"); return; }
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || inv.Has(item)) return;     // [CHANGED] той самий предмет удруге не даємо

        // [NEW] доставлений лист Поштмейстер повторно не видає
        LetterManager letters = LetterManager.Instance;
        if (letters != null && letters.IsDelivered(letters.FindByItem(item))) return;

        inv.Add(item);
    }

    // [NEW] Тег #sfx:назва — короткий звук через AudioManager
    private void PlaySound(string clipName)
    {
        AudioClip clip = sounds.Find(c => c != null && c.name == clipName);
        if (clip == null) { Debug.LogWarning($"DialogueManager: немає звуку «{clipName}» у списку sounds"); return; }
        AudioManager.PlaySfx(clip);
    }

    private Speaker FindSpeaker(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return speakers.Find(s => s.id == id);
    }
}
