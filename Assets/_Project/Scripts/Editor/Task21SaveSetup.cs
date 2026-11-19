#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Задача 21: меню LastLetter → Task 21: Add save to Sandbox_Player (спершу — Task 13 і 14).
// Заповнює InventoryManager.allItems, створює префаб SaveSystem, кладе його в Sandbox_Player
// і додає Sandbox_Player у Build Settings (без цього SceneLoader не завантажить сцену).
public static class Task21SaveSetup
{
    private const string SandboxPath = "Assets/_Project/Scenes/Sandbox/Sandbox_Player.unity";
    private const string ItemsDir = "Assets/_Project/ScriptableObjects/Items";
    private const string InventoryPrefab = "Assets/_Project/Prefabs/Managers/InventoryManager.prefab";
    private const string SaveSystemPrefab = "Assets/_Project/Prefabs/Managers/SaveSystem.prefab";

    [MenuItem("LastLetter/Task 21: Add save to Sandbox_Player")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefab) == null)
        {
            Debug.LogError("Спершу запусти Task 13 і Task 14 з меню LastLetter.");
            return;
        }

        // 1. InventoryManager має знати всі предмети, щоб відновити сумку зі збереження
        GameObject inv = PrefabUtility.LoadPrefabContents(InventoryPrefab);
        var so = new SerializedObject(inv.GetComponent<InventoryManager>());
        SerializedProperty list = so.FindProperty("allItems");
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemsDir });
        list.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(inv, InventoryPrefab);
        PrefabUtility.UnloadPrefabContents(inv);

        // 2. Префаб SaveSystem
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SaveSystemPrefab) == null)
        {
            var go = new GameObject("SaveSystem");
            go.AddComponent<SaveSystem>();
            Directory.CreateDirectory(Path.GetDirectoryName(SaveSystemPrefab));
            PrefabUtility.SaveAsPrefabAsset(go, SaveSystemPrefab);
            Object.DestroyImmediate(go);
        }

        // 3. SaveSystem у пісочницю
        var scene = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);
        if (Object.FindAnyObjectByType<SaveSystem>() == null)
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SaveSystemPrefab));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 4. Build Settings
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == SandboxPath))
            scenes.Add(new EditorBuildSettingsScene(SandboxPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("Задача 21: збереження додано. Play → візьми Свічку → Stop → Play → F9.");
    }
}
#endif
