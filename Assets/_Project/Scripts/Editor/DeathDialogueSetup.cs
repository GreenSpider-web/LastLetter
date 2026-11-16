#if UNITY_EDITOR
using System.Collections.Generic;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одним пунктом меню збирає сцену Sandbox_Death:
// камера, фон, GameState і діалог зі Смертю з портретами.
// Меню: LastLetter -> Створити сцену діалогу зі Смертю
public static class DeathDialogueSetup
{
    const string Root = "Assets/_Project/";
    const string InkPath = Root + "Ink/intro_dialogue.ink";   // [CHANGED] було death_dialogue.ink
    const string BgPath = Root + "Art/Backgrounds/bg_mezha_no_death.png";
    const string DeathPath = Root + "Art/Characters/portrait_death.png";
    const string LiliaPath = Root + "Art/Characters/portrait_lilia.png";
    const string ScenePath = Root + "Scenes/Sandbox/Sandbox_Death.unity";

    [MenuItem("LastLetter/Створити сцену діалогу зі Смертю")]
    static void CreateScene()
    {
        // картинки -> спрайти (фон 120 px на одиницю, як у GDD)
        MakeSprite(BgPath, 120);
        MakeSprite(DeathPath, 100);
        MakeSprite(LiliaPath, 100);

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Асети беремо ПІСЛЯ створення сцени: NewScene вивантажує все, на що ніщо не посилається,
        // і завантажене раніше посилання стає порожнім (через це раніше злітало поле Ink File)
        var ink = AssetDatabase.LoadAssetAtPath<InkFile>(InkPath);
        var bg = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        var death = AssetDatabase.LoadAssetAtPath<Sprite>(DeathPath);
        var lilia = AssetDatabase.LoadAssetAtPath<Sprite>(LiliaPath);

        var missing = new List<string>();
        if (ink == null) missing.Add(InkPath);
        if (bg == null) missing.Add(BgPath);
        if (death == null) missing.Add(DeathPath);
        if (lilia == null) missing.Add(LiliaPath);
        if (missing.Count > 0)
        {
            Debug.LogError("Не знайдено файли: " + string.Join(", ", missing));
            return;
        }

        // камера: 1080 px / 120 px на одиницю / 2 = 4.5
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // фон
        var bgGo = new GameObject("Background");
        var sr = bgGo.AddComponent<SpriteRenderer>();
        sr.sprite = bg;
        sr.sortingOrder = -10;

        new GameObject("GameState").AddComponent<GameState>();

        // діалог
        var view = new GameObject("Dialogue").AddComponent<InkDialogueView>();
        view.Setup(ink, new List<InkDialogueView.Speaker>
        {
            new InkDialogueView.Speaker { name = "Смерть", portrait = death, onLeft = false, nameColor = new Color(0.78f, 0.9f, 0.84f) },
            new InkDialogueView.Speaker { name = "Ліля", portrait = lilia, onLeft = true, nameColor = new Color(0.72f, 0.83f, 0.96f) },
        });
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log($"Сцену створено: {ScenePath}. Натисни Play.");
    }

    static void MakeSprite(string path, float pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;   // файлу немає, про це скажемо нижче

        bool ok = importer.textureType == TextureImporterType.Sprite
                  && importer.spriteImportMode == SpriteImportMode.Single
                  && Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit);
        if (ok) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
#endif
