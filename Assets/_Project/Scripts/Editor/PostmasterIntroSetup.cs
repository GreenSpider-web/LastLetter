#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Меню LastLetter → Add Postmaster intro to Sandbox_Player.
// Ставить Поштмейстера поруч із Лілею (спрайт postmaster_idle), вішає на нього розмову
// і IntroDialogue — вступ запускається сам на старті. Пантелеймону повертає panteleimon_letter.
// Працює з відкритою сценою Sandbox_Player; збережи потім Ctrl+S.
public static class PostmasterIntroSetup
{
    private const string SpritePath = "Assets/_Project/Art/Characters/postmaster_idle.PNG";
    private const float Height = 2.8f;     // трохи вищий за Лілю (2.5 од.)

    [MenuItem("LastLetter/Add Postmaster intro to Sandbox_Player")]
    public static void Build()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) { Debug.LogError("Відкрий сцену Sandbox_Player."); return; }

        Sprite sprite = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(SpritePath))
            if (o is Sprite s) { sprite = s; break; }
        if (sprite == null) { Debug.LogError("Не знайдено спрайт " + SpritePath); return; }

        // 1. Поштмейстер
        GameObject old = GameObject.Find("NPC_Postmaster");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var npc = new GameObject("NPC_Postmaster");
        Undo.RegisterCreatedObjectUndo(npc, "Add Postmaster");
        var sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        float scale = Height / sprite.bounds.size.y;
        npc.transform.localScale = new Vector3(scale, scale, 1f);

        // стоїть на тій самій лінії, що й Ліля, на 2.5 од. правіше (GDD: Поштмейстер x = 2.5 від входу)
        var playerSr = player.GetComponentInChildren<SpriteRenderer>();
        float feetY = playerSr != null ? playerSr.bounds.min.y : player.transform.position.y;
        float x = player.transform.position.x + 2.5f;
        npc.transform.position = new Vector3(x, feetY, 0f);
        npc.transform.position += new Vector3(0f, feetY - sr.bounds.min.y, 0f);
        if (playerSr != null) { sr.sortingLayerID = playerSr.sortingLayerID; sr.sortingOrder = playerSr.sortingOrder; }

        npc.AddComponent<BoxCollider2D>();

        var iso = new SerializedObject(npc.AddComponent<Interactable>());
        iso.FindProperty("displayName").stringValue = "Поштмейстер";
        iso.FindProperty("description").stringValue = "Худий чоловік у формі. Крізь круглі окуляри не видно очей.";
        iso.FindProperty("type").enumValueIndex = (int)InteractionType.Talk;
        iso.ApplyModifiedPropertiesWithoutUndo();

        // клік по ньому після вступу — коротка репліка
        var dso = new SerializedObject(npc.AddComponent<NPCDialogue>());
        dso.FindProperty("knot").stringValue = "postmaster_talk";
        dso.ApplyModifiedPropertiesWithoutUndo();

        npc.AddComponent<IntroDialogue>();

        // 2. Пантелеймону — його розмова
        GameObject pant = GameObject.Find("NPC_Panteleimon");
        if (pant != null && pant.GetComponent<NPCDialogue>() != null)
        {
            var pso = new SerializedObject(pant.GetComponent<NPCDialogue>());
            pso.FindProperty("knot").stringValue = "panteleimon_letter";
            pso.ApplyModifiedProperties();
        }

        Selection.activeGameObject = npc;
        EditorSceneManager.MarkSceneDirty(npc.scene);
        Debug.Log("Поштмейстера додано. Збережи сцену (Ctrl+S) і натисни Play — вступ почнеться сам.");
    }
}
#endif
