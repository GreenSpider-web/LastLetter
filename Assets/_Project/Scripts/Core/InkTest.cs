using System.Collections.Generic;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;

// Тест для задачі 2: показує діалог з Ink, вибір змінює прапорець у GameState.
// Простий інтерфейс через OnGUI, лише для перевірки. Справжній діалог — задача 14.
public class InkTest : MonoBehaviour
{
    [SerializeField] InkFile inkFile; // сюди перетягується сам test_dialogue.ink

    Story story;
    readonly List<string> lines = new List<string>();

    void Start()
    {
        if (inkFile == null || !inkFile.isCompiled)
        {
            Debug.LogError("InkTest: не вказано .ink файл або він не скомпілювався");
            enabled = false;
            return;
        }
        story = new Story(inkFile.storyJson);

        // 1) GameState -> Ink: передаємо поточні значення прапорців у змінні Ink
        foreach (var flag in GameState.Instance.AllFlags)
            if (story.variablesState.GlobalVariableExistsWithName(flag))
                story.variablesState[flag] = GameState.Instance.GetFlag(flag);

        // 2) Ink -> GameState: коли в Ink виконується "~ prologue_done = true",
        //    значення одразу записується в GameState
        foreach (var flag in GameState.Instance.AllFlags)
            if (story.variablesState.GlobalVariableExistsWithName(flag))
                story.ObserveVariable(flag, (name, value) =>
                    GameState.Instance.SetFlag(name, (bool)value));

        ContinueStory();
    }

    void ContinueStory()
    {
        while (story.canContinue)
            lines.Add(story.Continue().Trim());
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 700, 500));
        foreach (var line in lines) GUILayout.Label(line);

        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            if (GUILayout.Button(story.currentChoices[i].text))
            {
                story.ChooseChoiceIndex(i);
                ContinueStory();
                break;
            }
        }

        if (!story.canContinue && story.currentChoices.Count == 0)
            GUILayout.Label($"Кінець. prologue_done = {GameState.Instance.GetFlag("prologue_done")}");
        GUILayout.EndArea();
    }
}
