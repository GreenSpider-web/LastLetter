using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;

// Діалог як у візуальних новелах: панель з текстом унизу,
// портрет того, хто говорить, над панеллю (справа або зліва),
// текст друкується по літері, клік / Space / Enter — далі.
// Ім'я мовця береться з рядка Ink до двокрапки: "Смерть: Привіт." -> мовець "Смерть".
// Або з тегів (як у LastLetter_Ch1.ink): #speaker:postmaster — хто говорить, #emotion:angry — вираз обличчя.
// Тестова версія на OnGUI; справжній DialogueManager на Canvas — задача 14.
public class InkDialogueView : MonoBehaviour
{
    [Serializable]
    public class Speaker
    {
        public string name = "Смерть";      // як у .ink перед двокрапкою; для тегів — що показати
        public string id = "";              // як у тегу #speaker:postmaster (якщо .ink з тегами)
        public Sprite portrait;             // звичайний вираз
        public bool onLeft;                 // false = справа
        public Color nameColor = Color.white;
        public List<Emotion> emotions = new List<Emotion>();   // інші вирази: name — як у #emotion:angry

        public Sprite GetPortrait(string emotion)
        {
            Emotion e = emotions.Find(x => x.name == emotion);
            return e != null && e.portrait != null ? e.portrait : portrait;
        }
    }

    [Serializable]
    public class Emotion
    {
        public string name = "angry";
        public Sprite portrait;
    }

    [SerializeField] InkFile inkFile;
    [SerializeField] string startKnot = "";        // з якого вузла почати (порожньо — з початку файлу)
    [SerializeField] bool showBothPortraits = false;   // показувати обох: хто мовчить — притемнений
    [SerializeField, HideInInspector] string inkAssetPath;   // запасний шлях, щоб у редакторі знайти .ink, якщо поле злетить
    [SerializeField] bool showPlayerAnswer = false; // показувати вибрану відповідь окремою реплікою
    [SerializeField] string playerName = "Ліля";   // так підписуються відповіді гравця
    [SerializeField] List<Speaker> speakers = new List<Speaker>();

    [Header("Вигляд")]
    [SerializeField] int fontSize = 26;
    [SerializeField] float charsPerSecond = 45f;
    [SerializeField, Range(0.15f, 0.5f)] float boxHeight = 0.27f;      // частка висоти екрана
    [SerializeField, Range(0.3f, 0.9f)] float portraitHeight = 0.62f;  // частка висоти екрана
    [SerializeField] Color boxColor = new Color(0.04f, 0.05f, 0.07f, 0.88f);
    [SerializeField] Color textColor = new Color(0.91f, 0.89f, 0.84f);

    static readonly Regex SpeakerRegex = new Regex(@"^([^:]{1,30}):\s*(.*)$");

    Story story;
    string speakerName = "";
    string lineText = "";
    bool narrator;              // рядок оповідача (#speaker:narrator) — курсивом
    Sprite currentSprite;       // портрет того, хто говорить
    Sprite leftSprite, rightSprite;   // останні портрети зліва і справа (для showBothPortraits)
    float shownChars;
    GUIStyle textStyle, nameStyle, choiceStyle, hintStyle;
    int stylesForHeight = -1;   // для якої висоти екрана створені стилі
    int fs;                     // розмір шрифту з урахуванням розміру вікна

    bool IsTyping => shownChars < lineText.Length;
    bool HasChoices => story.currentChoices.Count > 0;

#if UNITY_EDITOR
    // Викликає скрипт DeathDialogueSetup, коли збирає сцену
    public void Setup(InkFile ink, List<Speaker> list, string knot = "", bool bothPortraits = false)
    {
        inkFile = ink;
        inkAssetPath = UnityEditor.AssetDatabase.GetAssetPath(ink);
        speakers = list;
        startKnot = knot;
        showBothPortraits = bothPortraits;
    }
#endif

    void Start()
    {
#if UNITY_EDITOR
        if (inkFile == null && !string.IsNullOrEmpty(inkAssetPath))
            inkFile = UnityEditor.AssetDatabase.LoadAssetAtPath<InkFile>(inkAssetPath);
#endif
        if (inkFile == null || !inkFile.isCompiled)
        {
            Debug.LogError("InkDialogueView: не вказано .ink файл або він не скомпілювався");
            enabled = false;
            return;
        }
        story = new Story(inkFile.storyJson);
        ConnectGameState();
        if (!string.IsNullOrEmpty(startKnot)) story.ChoosePathString(startKnot);
        NextLine();
    }

    // Ті самі зв'язки, що в InkTest: прапорці GameState <-> змінні Ink
    void ConnectGameState()
    {
        var gs = GameState.Instance;
        if (gs == null) { Debug.LogWarning("InkDialogueView: у сцені немає GameState"); return; }

        foreach (var flag in gs.AllFlags)
        {
            if (!story.variablesState.GlobalVariableExistsWithName(flag)) continue;
            story.variablesState[flag] = gs.GetFlag(flag);
            story.ObserveVariable(flag, (name, value) => gs.SetFlag(name, (bool)value));
        }
    }

    void NextLine()
    {
        while (story.canContinue)
        {
            string raw = story.Continue().Trim();
            ReadTags(story.currentTags, out string tagSpeaker, out string emotion);
            if (raw.Length == 0) continue;

            narrator = tagSpeaker == "narrator";
            var match = SpeakerRegex.Match(raw);
            if (tagSpeaker.Length > 0)
            {
                Speaker byId = speakers.Find(s => s.id == tagSpeaker);
                speakerName = byId != null ? byId.name : "";
                lineText = raw;
            }
            else if (match.Success)
            {
                speakerName = match.Groups[1].Value.Trim();
                lineText = match.Groups[2].Value;
            }
            else
            {
                speakerName = "";   // рядок без імені — опис / оповідач
                lineText = raw;
            }

            // портрет під емоцію; запам'ятати, хто стоїть зліва і справа
            Speaker speaker = FindSpeaker(speakerName);
            currentSprite = speaker != null ? speaker.GetPortrait(emotion) : null;
            if (speaker != null && currentSprite != null)
            {
                if (speaker.onLeft) leftSprite = currentSprite;
                else rightSprite = currentSprite;
            }

            shownChars = 0;
            return;
        }
    }

    // #speaker:… і #emotion:… з поточного рядка
    static void ReadTags(List<string> tags, out string speaker, out string emotion)
    {
        speaker = "";
        emotion = "";
        foreach (string tag in tags)
        {
            string[] parts = tag.Split(':');
            if (parts.Length < 2) continue;
            if (parts[0].Trim() == "speaker") speaker = parts[1].Trim();
            else if (parts[0].Trim() == "emotion") emotion = parts[1].Trim();
        }
    }

    void Choose(int index)
    {
        string answer = story.currentChoices[index].text;
        story.ChooseChoiceIndex(index);

        if (showPlayerAnswer)
        {
            // показуємо відповідь гравця як його репліку
            speakerName = playerName;
            lineText = answer;
            shownChars = 0;
        }
        else
        {
            // одразу наступна репліка співрозмовника
            speakerName = "";
            lineText = "";
            NextLine();
        }
    }

    void Advance()
    {
        if (IsTyping) shownChars = lineText.Length;   // перший клік — дописати рядок
        else if (story.canContinue) NextLine();       // другий — наступний рядок
    }

    void Update()
    {
        if (story != null && IsTyping) shownChars += charsPerSecond * Time.deltaTime;
    }

    void OnGUI()
    {
        if (story == null) return;
        CreateStyles();

        float margin = 30f;
        float h = Screen.height * boxHeight;
        var box = new Rect(margin, Screen.height - h - margin, Screen.width - 2 * margin, h);

        Speaker speaker = FindSpeaker(speakerName);

        // 1) портрет — до панелі, щоб панель трохи його перекривала знизу
        if (showBothPortraits)
        {
            // хто говорить — яскравий, хто мовчить — притемнений
            if (leftSprite != null) DrawPortrait(leftSprite, true, box, speaker == null || !speaker.onLeft);
            if (rightSprite != null) DrawPortrait(rightSprite, false, box, speaker == null || speaker.onLeft);
        }
        else if (speaker != null && currentSprite != null) DrawPortrait(currentSprite, speaker.onLeft, box, false);

        // 2) панель
        GUI.color = boxColor;
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 3) ім'я
        float nameH = fs * 1.7f;
        if (speakerName.Length > 0)
        {
            nameStyle.normal.textColor = speaker != null ? speaker.nameColor : textColor;
            GUI.Label(new Rect(box.x + 28, box.y + 14, box.width - 56, nameH), speakerName, nameStyle);
        }

        // 4) текст, що друкується
        var textRect = new Rect(box.x + 28, box.y + 18 + nameH, box.width - 56, box.height - nameH - 40);
        int visible = Mathf.Clamp(Mathf.FloorToInt(shownChars), 0, lineText.Length);
        string shown = lineText.Substring(0, visible);
        GUI.Label(textRect, narrator ? "<i>" + shown + "</i>" : shown, textStyle);

        bool showChoices = !IsTyping && !story.canContinue && HasChoices;
        bool ended = !IsTyping && !story.canContinue && !HasChoices;

        // 5) підказка внизу праворуч
        if (!IsTyping && !showChoices)
            GUI.Label(new Rect(box.x, box.y, box.width - 24, box.height - 12), ended ? "Кінець" : "▼", hintStyle);

        // 6) варіанти відповіді — над панеллю зліва
        if (showChoices)
        {
            float gap = 8f;
            float width = box.width * 0.7f;
            int count = story.currentChoices.Count;

            // висота кожної кнопки під її текст: довгий варіант переноситься на новий рядок
            var labels = new GUIContent[count];
            var heights = new float[count];
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                labels[i] = new GUIContent(story.currentChoices[i].text);
                heights[i] = Mathf.Max(fs * 1.9f, choiceStyle.CalcHeight(labels[i], width));
                total += heights[i] + gap;
            }

            float y = box.y - total - 2f;
            for (int i = 0; i < count; i++)
            {
                if (GUI.Button(new Rect(box.x, y, width, heights[i]), labels[i], choiceStyle))
                {
                    Choose(i);
                    break;
                }
                y += heights[i] + gap;
            }
            return;   // поки є вибір, кліки по екрану не гортають діалог
        }

        // 7) клік / Space / Enter — далі
        var e = Event.current;
        bool click = e.type == EventType.MouseDown && e.button == 0;
        bool key = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return);
        if (click || key)
        {
            Advance();
            e.Use();
        }
    }

    Speaker FindSpeaker(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var s in speakers)
            if (!string.IsNullOrEmpty(s.name) && name.StartsWith(s.name, StringComparison.OrdinalIgnoreCase))
                return s;   // "Марка (пошепки)" теж знайде "Марка"
        return null;
    }

    void DrawPortrait(Sprite sprite, bool onLeft, Rect box, bool dim)
    {
        Texture tex = sprite.texture;
        Rect tr = sprite.textureRect;

        float h = Screen.height * portraitHeight;
        float w = h * tr.width / tr.height;
        float x = onLeft ? box.x + 20 : box.xMax - w - 20;
        var rect = new Rect(x, box.y - h + box.height * 0.12f, w, h);

        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        GUI.color = dim ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;
        GUI.DrawTextureWithTexCoords(rect, tex, uv);
        GUI.color = Color.white;
    }

    void CreateStyles()
    {
        if (stylesForHeight == Screen.height) return;
        stylesForHeight = Screen.height;

        // Font Size задано для екрана висотою 1080; у меншому вікні шрифт зменшується пропорційно
        fs = Mathf.Max(12, Mathf.RoundToInt(fontSize * Screen.height / 1080f));

        textStyle = new GUIStyle(GUI.skin.label) { fontSize = fs, wordWrap = true, richText = true };
        textStyle.normal.textColor = textColor;

        nameStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(fs * 1.1f), fontStyle = FontStyle.Bold };

        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = fs, alignment = TextAnchor.LowerRight };
        hintStyle.normal.textColor = new Color(textColor.r, textColor.g, textColor.b, 0.6f);

        choiceStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fs,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            padding = new RectOffset(16, 16, 8, 8)
        };
        choiceStyle.normal.textColor = textColor;
        choiceStyle.hover.textColor = Color.white;
    }
}
