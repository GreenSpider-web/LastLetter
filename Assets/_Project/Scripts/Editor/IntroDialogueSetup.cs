#if UNITY_EDITOR
using System.Collections.Generic;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Одним пунктом меню збирає сцену Sandbox_Intro (як Sandbox_Death):
// камера, фон Площі Пошти, GameState і вступний діалог Поштмейстера з Лілею
// (вузол postmaster_intro з LastLetter_Ch1.ink) з портретами під емоції.
// Меню: LastLetter -> Створити сцену вступу з Поштмейстером
public static class IntroDialogueSetup
{
    const string Root = "Assets/_Project/";
    const string InkPath = Root + "Ink/LastLetter_Ch1.ink";
    const string BgPath = Root + "Art/Backgrounds/bg_intro_postoffice.png";   // 1920×1080, розмита Площа Пошти
    const string Portraits = Root + "Art/Characters/Portraits/";
    const string ScenePath = Root + "Scenes/Sandbox/Sandbox_Intro.unity";

    [MenuItem("LastLetter/Створити сцену вступу з Поштмейстером")]
    static void CreateScene()
    {
        MakeSprite(BgPath, 120);    // 1080 px / 120 = 9 од. — рівно під камеру 4.5

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // асети — після створення сцени (див. коментар у DeathDialogueSetup)
        var ink = AssetDatabase.LoadAssetAtPath<InkFile>(InkPath);
        Sprite bg = LoadSprite(BgPath);
        if (ink == null || bg == null)
        {
            Debug.LogError($"Не знайдено {(ink == null ? InkPath : BgPath)}");
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

        var bgGo = new GameObject("Background");
        var sr = bgGo.AddComponent<SpriteRenderer>();
        sr.sprite = bg;
        sr.sortingOrder = -10;
        bgGo.AddComponent<BackgroundFit>();     // без чорних смуг за будь-якого розміру вікна

        new GameObject("GameState").AddComponent<GameState>();

        // Ліля зліва, Поштмейстер справа; Марка без портрета (лише ім'я)
        var lilia = new InkDialogueView.Speaker
        {
            name = "Ліля", id = "lilia", onLeft = true, nameColor = new Color(0.72f, 0.83f, 0.96f),
            portrait = LoadSprite(Portraits + "portrait_lilia_neutral.png"),
            emotions = Emotions("lilia", "sad", "scared", "surprised", "confused"),
        };
        var postmaster = new InkDialogueView.Speaker
        {
            name = "Поштмейстер", id = "postmaster", onLeft = false, nameColor = new Color(0.85f, 0.7f, 0.65f),
            portrait = LoadSprite(Portraits + "portrait_postmaster_neutral.png"),
            emotions = Emotions("postmaster", "neutral", "smile", "angry"),
        };
        var marka = new InkDialogueView.Speaker
        {
            name = "Марка", id = "marka", onLeft = false, nameColor = new Color(0.95f, 0.85f, 0.6f),
        };

        var view = new GameObject("Dialogue").AddComponent<InkDialogueView>();
        view.Setup(ink, new List<InkDialogueView.Speaker> { lilia, postmaster, marka },
                   "postmaster_intro", true, "Sandbox_Player");   // після вступу — у гру (L1_PostSquare, коли буде)
        var vso = new SerializedObject(view);
        vso.FindProperty("boxColor").colorValue = new Color(0.06f, 0.05f, 0.05f, 1f);   // суцільна панель
        vso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log($"Сцену створено: {ScenePath}. Натисни Play.");
    }

    static List<InkDialogueView.Emotion> Emotions(string who, params string[] names)
    {
        var list = new List<InkDialogueView.Emotion>();
        foreach (string n in names)
        {
            Sprite s = LoadSprite($"{Portraits}portrait_{who}_{n}.png");
            if (s != null) list.Add(new InkDialogueView.Emotion { name = n, portrait = s });
            else Debug.LogWarning($"Немає портрета {who} / {n}");
        }
        return list;
    }

    static void MakeSprite(string path, float pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType == TextureImporterType.Sprite &&
            importer.spriteImportMode == SpriteImportMode.Single &&
            Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // Найбільший спрайт у файлі (картинки імпортовані як Multiple — там буває дрібний зайвий шматок)
    static Sprite LoadSprite(string path)
    {
        Sprite best = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s && (best == null || s.rect.width * s.rect.height > best.rect.width * best.rect.height))
                best = s;
        return best;
    }
}
#endif
