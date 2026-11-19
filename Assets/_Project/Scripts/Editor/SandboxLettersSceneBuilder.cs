#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// Збирає сцену Sandbox_Letters: меню LastLetter → Create Sandbox_Letters scene.
// Два листи (fisher, tailor) і двоє NPC із сірих квадратів.
public static class SandboxLettersSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Letters.unity";
    private const string LettersDir = "Assets/_Project/ScriptableObjects/Letters/";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";

    [MenuItem("LastLetter/Create Sandbox_Letters scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ассети беремо після створення сцени, щоб посилання не лишилися порожніми
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        if (square == null)
        {
            Debug.LogError("Немає " + SquarePath + ". Спершу запустіть LastLetter → Create Sandbox_Steam scene.");
            return;
        }

        LetterData fisher = GetOrCreateLetter("Letter_Fisher", "fisher", "Лист рибалці",
            "pantelejmon", "Рибалка Пантелеймон", "read_letter_fisher");
        LetterData tailor = GetOrCreateLetter("Letter_Tailor", "tailor", "Лист кравчині",
            "stefaniia", "Кравчиня Стефанія", "read_letter_tailor");

        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        new GameObject("GameState").AddComponent<GameState>();

        var managerObj = new GameObject("LetterManager");
        var manager = managerObj.AddComponent<LetterManager>();
        var managerSo = new SerializedObject(manager);
        SerializedProperty list = managerSo.FindProperty("letters");
        list.arraySize = 2;
        list.GetArrayElementAtIndex(0).objectReferenceValue = fisher;
        list.GetArrayElementAtIndex(1).objectReferenceValue = tailor;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        MakeNpc("NPC_Pantelejmon", "pantelejmon", square, new Vector3(-3f, 0f, 0f), new Color(0.4f, 0.6f, 0.8f));
        MakeNpc("NPC_Stefaniia", "stefaniia", square, new Vector3(3f, 0f, 0f), new Color(0.8f, 0.5f, 0.6f));

        new GameObject("SandboxLetterClicker").AddComponent<SandboxLetterClicker>();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Сцену Sandbox_Letters створено: " + ScenePath);
    }

    private static void MakeNpc(string name, string addresseeId, Sprite sprite, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.2f, 2f, 1f);
        go.AddComponent<BoxCollider2D>();   // після спрайта: розмір колайдера береться зі спрайта

        var npc = go.AddComponent<NPCAddressee>();
        var so = new SerializedObject(npc);
        so.FindProperty("addresseeId").stringValue = addresseeId;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static LetterData GetOrCreateLetter(string file, string id, string title,
                                                string addresseeId, string address, string readFlag)
    {
        string path = LettersDir + file + ".asset";
        var letter = AssetDatabase.LoadAssetAtPath<LetterData>(path);
        if (letter == null)
        {
            Directory.CreateDirectory(LettersDir);
            letter = ScriptableObject.CreateInstance<LetterData>();
            AssetDatabase.CreateAsset(letter, path);
        }

        var so = new SerializedObject(letter);
        so.FindProperty("letterId").stringValue = id;
        so.FindProperty("title").stringValue = title;
        so.FindProperty("addresseeId").stringValue = addresseeId;
        so.FindProperty("addressDescription").stringValue = address;
        so.FindProperty("readFlag").stringValue = readFlag;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(letter);
        return letter;
    }
}
#endif
