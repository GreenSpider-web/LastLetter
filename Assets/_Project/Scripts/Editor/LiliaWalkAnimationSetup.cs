using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Задача 19: анімація Лілі — ходьба (Art/Items/Walk_lily_*) і дихання на місці (Art/Characters/lilia_idle_01…04).
// Меню LastLetter → «Task 19: Анімація Лілі (ходьба і дихання)» робить усе за раз:
//  1. налаштовує імпорт кадрів (один спрайт на картинку, спільна точка опори — щоб Ліля не «стрибала» між кадрами);
//  2. створює кліпи Lilia_Idle (дихає і зрідка кліпає) і Lilia_Walk (кадри ходьби по колу);
//  3. створює Animator Controller з параметром Speed (його вже передає PlayerController):
//     Speed > 0.1 → Walk, Speed < 0.1 → Idle;
//  4. вішає Animator на Player/Visual у префабі і вимикає тимчасовий WalkBob (підстрибування кодом).
// Запустити ще раз можна — усе перезапишеться.
public static class LiliaWalkAnimationSetup
{
    private const string FramesFolder = "Assets/_Project/Art/Items/";
    private const string IdleFolder = "Assets/_Project/Art/Characters/";
    private const string AnimFolder = "Assets/_Project/Art/Animations";
    private const string PlayerPrefab = "Assets/_Project/Prefabs/Characters/Player.prefab";

    // Кадри ходьби по порядку. Walk_lily_5 прибрали — Ліля на ньому менша, голова «смикалась» униз.
    private static readonly int[] WalkOrder = { 1, 2, 3, 4, 6 };
    private const float FramesPerSecond = 8f;   // ходьба: 5 кадрів = 2 кроки за 0.6 с

    // Дихання: 2-3-4-3 (вдих-видих) двічі, потім 1 — заплющені очі (кліпнула). Повільніше за ходьбу.
    private static readonly int[] IdleOrder = { 2, 3, 4, 3, 2, 3, 4, 3, 1 };
    private const float IdleFramesPerSecond = 4f;   // 9 кадрів ≈ 2.25 с на коло

    // Ліля на нових кадрах (ходьба і дихання) ≈1990 px заввишки, на старому чорноволосому спрайті було ≈1800 px,
    // тому 110 пікселів на одиницю замість 100 — тоді зріст на сцені такий самий, як був.
    private const float WalkPixelsPerUnit = 110f;
    // Точка опори кадру = середина фігури старого спрайта: ноги ≈140 px від низу + половина зросту (9 од. × 110 px).
    // Тоді Visual не треба зсувати, а ноги на всіх кадрах стоять на одному рівні.
    private static readonly Vector2 WalkPivot = new Vector2(0.5f, 0.48f);

    [MenuItem("LastLetter/Task 19: Анімація Лілі (ходьба і дихання)")]
    public static void Setup()
    {
        // 1. Кадри ходьби і дихання
        Sprite[] walkFrames = LoadFrames(WalkOrder, n => FramesFolder + "Walk_lily_" + n + ".PNG");
        Sprite[] idleFrames = LoadFrames(IdleOrder, n => IdleFolder + "lilia_idle_0" + n + ".png.PNG");
        if (walkFrames == null || idleFrames == null) return;

        // 2. Кліпи
        if (!AssetDatabase.IsValidFolder(AnimFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Art", "Animations");

        AnimationClip idle = MakeClip(AnimFolder + "/Lilia_Idle.anim", idleFrames, IdleFramesPerSecond);
        AnimationClip walk = MakeClip(AnimFolder + "/Lilia_Walk.anim", walkFrames, FramesPerSecond);

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        Transform visual = root.transform.Find("Visual");
        visual.GetComponent<SpriteRenderer>().sprite = idleFrames[0];   // і в редакторі одразу видно руду Лілю

        // 3. Animator Controller: Idle ⇄ Walk за параметром Speed
        string controllerPath = AnimFolder + "/Lilia.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idle;
        AnimatorState walkState = sm.AddState("Walk");
        walkState.motion = walk;
        sm.defaultState = idleState;

        AddTransition(idleState, walkState, AnimatorConditionMode.Greater);
        AddTransition(walkState, idleState, AnimatorConditionMode.Less);

        // 4. Animator на Visual, посилання в PlayerController, WalkBob вимкнути
        Animator animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var player = root.GetComponent<PlayerController>();
        var so = new SerializedObject(player);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.ApplyModifiedPropertiesWithoutUndo();

        WalkBob bob = visual.GetComponent<WalkBob>();
        if (bob != null) bob.enabled = false;

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();

        Debug.Log("Анімація Лілі готова: " + controllerPath + " — ходьба " + walkFrames.Length + " кадрів, дихання " + idleFrames.Length + " кадрів");
    }

    // Налаштувати імпорт кадрів і зібрати спрайти в потрібному порядку (номер може повторюватись)
    private static Sprite[] LoadFrames(int[] order, System.Func<int, string> pathOf)
    {
        var frames = new Sprite[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            string path = pathOf(order[i]);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogError("Немає кадру " + path); return null; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = WalkPixelsPerUnit;
            importer.maxTextureSize = 4096;   // кадр 1327×2360 — щоб не стискався до 2048

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = WalkPivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return frames;
    }

    // Кліп, що по черзі міняє спрайт у SpriteRenderer і крутиться по колу
    private static AnimationClip MakeClip(string path, Sprite[] frames, float fps)
    {
        var clip = new AnimationClip { frameRate = fps };

        // останній ключ повторює перший кадр — щоб останній кадр показувався стільки ж, скільки інші
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] };

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // Перехід без очікування кінця кліпу: одразу, як тільки Speed перейшла поріг
    private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(mode, 0.1f, "Speed");
    }
}
