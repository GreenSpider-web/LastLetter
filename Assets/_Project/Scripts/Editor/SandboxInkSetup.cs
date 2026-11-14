#if UNITY_EDITOR
using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Задача 2: одним пунктом меню створює сцену Sandbox_Ink з об'єктами GameState і InkTest
// і сам підставляє test_dialogue.ink.
public static class SandboxInkSetup
{
    const string InkPath = "Assets/_Project/Ink/test_dialogue.ink";
    const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Ink.unity";

    [MenuItem("LastLetter/Задача 2: створити сцену Sandbox_Ink")]
    static void CreateScene()
    {
        var ink = AssetDatabase.LoadAssetAtPath<InkFile>(InkPath);
        if (ink == null || !ink.isCompiled)
        {
            Debug.LogError($"Не вдалося взяти скомпільований {InkPath}. Перевір, що файл є і без помилок Ink.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        new GameObject("GameState").AddComponent<GameState>();

        var inkTest = new GameObject("InkTest").AddComponent<InkTest>();
        var so = new SerializedObject(inkTest);
        so.FindProperty("inkFile").objectReferenceValue = ink;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Сцену збережено: {ScenePath}. Натисни Play.");
    }
}
#endif
