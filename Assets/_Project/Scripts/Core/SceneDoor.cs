using UnityEngine;

// Двері / арка між ділянками: Ліля підходить, клікає — затемнення і інша сцена (SceneLoader).
// Можна зачинити прапорцем GameState: поки його немає — лише репліка (напр. арка до кінця пролога).
[RequireComponent(typeof(Interactable))]
public class SceneDoor : MonoBehaviour
{
    [Tooltip("Назва сцени, куди веде, напр. L1_TailorHouse (має бути в Build Settings)")]
    [SerializeField] private string targetScene = "L1_TailorStreet";
    [Tooltip("Де з'явиться Ліля в новій сцені")]
    [SerializeField] private Vector2 spawnPoint = new Vector2(1.5f, -3.5f);

    [Header("Зачинено")]
    [Tooltip("Без цього прапорця двері не відчиняються. Порожньо — відчинені завжди")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField] private string lockedText = "Зачинено.";

    private void Awake()
    {
        GetComponent<Interactable>().onInteract.AddListener(Open);
    }

    public void Open()
    {
        bool locked = !string.IsNullOrEmpty(requiredFlag) &&
                      (GameState.Instance == null || !GameState.Instance.GetFlag(requiredFlag));
        if (locked)
        {
            if (InteractionText.Instance != null) InteractionText.Instance.Show(lockedText);
            return;
        }
        SceneLoader.Load(targetScene, spawnPoint);
    }
}
