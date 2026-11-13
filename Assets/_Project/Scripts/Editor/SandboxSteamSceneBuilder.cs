#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// Збирає сцену Sandbox_Steam із сірих квадратів: меню LastLetter → Create Sandbox_Steam scene.
// Після того як сцена збережена, цей скрипт більше не потрібен.
public static class SandboxSteamSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Steam.unity";
    private const string DataPath = "Assets/_Project/ScriptableObjects/Letters/SteamMinigameData.asset";
    private const string SquarePath = "Assets/_Project/Art/UI/gray_square.png";

    [MenuItem("LastLetter/Create Sandbox_Steam scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ассети беремо після створення сцени: за першого запуску поле Data лишилося порожнім
        Sprite square = GetOrCreateSquareSprite();
        SteamMinigameData data = GetOrCreateData();

        // Камера: 1080 px висоти / 120 px на одиницю = 9 одиниць, тому розмір 4.5
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        // Чайник (сірий квадрат) внизу по центру
        MakeSquare("Kettle", square, new Vector3(0f, -3f, 0f), new Vector3(1.5f, 1f, 1f),
                   new Color(0.45f, 0.45f, 0.45f), 0);

        // Колона пари: ширина = 2 * columnHalfWidthPx / pixelsPerUnit
        float columnWidth = 2f * data.columnHalfWidthPx / data.pixelsPerUnit;
        GameObject steam = MakeSquare("SteamColumn", square, new Vector3(0f, -0.5f, 0f),
                   new Vector3(columnWidth, 5f, 1f), new Color(0.85f, 0.9f, 1f, 0.55f), 1);

        // Конверт (світлий квадрат), їде за мишею
        GameObject envelope = MakeSquare("Envelope", square, new Vector3(0f, 1.5f, 0f),
                   new Vector3(0.7f, 0.45f, 1f), new Color(0.95f, 0.85f, 0.6f), 2);

        // Керування
        var manager = new GameObject("SteamMinigame");
        var minigame = manager.AddComponent<SteamMinigame>();
        var so = new SerializedObject(minigame);
        so.FindProperty("data").objectReferenceValue = data;
        so.FindProperty("steamColumn").objectReferenceValue = steam.transform;
        so.FindProperty("envelope").objectReferenceValue = envelope.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Сцену Sandbox_Steam створено: " + ScenePath);
    }

    private static GameObject MakeSquare(string name, Sprite sprite, Vector3 pos, Vector3 scale,
                                         Color color, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        go.transform.position = pos;
        go.transform.localScale = scale;
        return go;
    }

    // Біла текстура 4×4, яка стає спрайтом розміром 1×1 одиниця
    private static Sprite GetOrCreateSquareSprite()
    {
        if (!File.Exists(SquarePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SquarePath));
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            File.WriteAllBytes(SquarePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(SquarePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SquarePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
    }

    private static SteamMinigameData GetOrCreateData()
    {
        var data = AssetDatabase.LoadAssetAtPath<SteamMinigameData>(DataPath);
        if (data == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
            data = ScriptableObject.CreateInstance<SteamMinigameData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        return data;
    }
}
#endif
