#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Меню LastLetter → Marka: companion near Lilia.
// 1. Створює префаб Companion_Marka (спрайт журавлика + MarkaCompanion).
// 2. Кладе його всередину префаба Player — тепер Марка з'являється в кожній сцені, де є Ліля.
// Меню можна запускати повторно: удруге Марка в Player не додається.
public static class MarkaCompanionSetup
{
    private const string SpritePath = "Assets/_Project/Art/Characters/marka_fly.PNG";
    private const string MarkaPrefabPath = "Assets/_Project/Prefabs/Characters/Companion_Marka.prefab";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";

    [MenuItem("LastLetter/Marka: companion near Lilia")]
    public static void Build()
    {
        Sprite sprite = FindMarkaSprite();
        if (sprite == null) { Debug.LogError($"Не знайшов спрайт Марки: {SpritePath}"); return; }

        GameObject markaPrefab = CreateMarkaPrefab(sprite);
        AddToPlayerPrefab(markaPrefab);
        Debug.Log("Марка готовий: летить за Лілею в кожній сцені з префабом Player. Натисни Play.");
    }

    // Авто-нарізка marka_fly.PNG дала журавлика і кілька крихітних шматочків — беремо найбільший
    private static Sprite FindMarkaSprite()
    {
        return AssetDatabase.LoadAllAssetsAtPath(SpritePath)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height)
            .FirstOrDefault();
    }

    private static GameObject CreateMarkaPrefab(Sprite sprite)
    {
        var go = new GameObject("Companion_Marka");
        go.transform.localScale = Vector3.one * (0.8f / sprite.bounds.size.x);   // щоб у редакторі не був велетнем
        go.AddComponent<SpriteRenderer>().sprite = sprite;
        go.AddComponent<MarkaCompanion>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, MarkaPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void AddToPlayerPrefab(GameObject markaPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (root.GetComponentInChildren<MarkaCompanion>(true) == null)
        {
            var marka = (GameObject)PrefabUtility.InstantiatePrefab(markaPrefab, root.transform);
            marka.transform.localPosition = new Vector3(-1f, 2.2f, 0f);   // лише для редактора; у грі Марка ставить себе сам
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        PrefabUtility.UnloadPrefabContents(root);
    }
}
#endif
