using UnityEngine;

// Рецепт поєднання двох предметів у сумці: A + B → результат.
// У зрізі один рецепт: Свічка + Сірники → Запалена свічка.
[CreateAssetMenu(fileName = "Recipe", menuName = "LastLetter/Combine Recipe")]
public class CombineRecipe : ScriptableObject
{
    public ItemData itemA;
    public ItemData itemB;
    public ItemData result;

    [Tooltip("Репліка, коли поєднання вдалося")]
    public string successText = "Вийшло!";

    // Порядок не важливий: Свічка + Сірники = Сірники + Свічка
    public bool Matches(ItemData a, ItemData b)
    {
        return (a == itemA && b == itemB) || (a == itemB && b == itemA);
    }
}
