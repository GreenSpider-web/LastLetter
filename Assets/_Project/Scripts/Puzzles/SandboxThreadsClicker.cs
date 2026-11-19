using UnityEngine;

// Тестовий стенд для задачі 23 (сцена Sandbox_Threads). Це не частина гри:
// кнопки відкривають пазли, показують, які розв'язано, і викликають підказку Марки (рівень 3).
public class SandboxThreadsClicker : MonoBehaviour
{
    private ThreadPuzzleTrigger[] triggers;
    private ThreadPuzzleUI puzzleUI;
    private string message = "";

    private void Start()
    {
        triggers = FindObjectsByType<ThreadPuzzleTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        puzzleUI = FindAnyObjectByType<ThreadPuzzleUI>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 420, 300));

        if (puzzleUI != null && puzzleUI.IsOpen)
        {
            if (GUILayout.Button("Підказка Марки (рівень 3)"))
                message = puzzleUI.HighlightWrongTile() ? "Підсвічено плитку, повернуту не так" : "Усі плитки шляху стоять правильно";
            GUILayout.Label("ЛКМ по плитці — повернути. ПКМ — закрити.");
        }
        else
        {
            foreach (ThreadPuzzleTrigger t in triggers)
            {
                string state = t.IsSolved ? " [розв'язано]" : "";
                if (GUILayout.Button("Відкрити «" + t.Data.Title + "»" + state) && !t.IsSolved) t.Open();
            }
            if (GUILayout.Button("Скинути все"))
            {
                if (GameState.Instance != null) GameState.Instance.ResetAll();
                foreach (ThreadPuzzleTrigger t in triggers) t.gameObject.SetActive(true);
                message = "Пазли скинуто";
            }
        }

        GUILayout.Label(message);
        GUILayout.EndArea();
    }
}
