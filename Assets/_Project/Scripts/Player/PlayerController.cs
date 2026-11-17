using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Рух Лілі кліком: ЛКМ по землі -> Ліля йде в цю точку в межах WalkArea.
// Чим вище по екрану (далі від камери), тим Ліля менша.
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField] private PlayerMovementData data;       // швидкість і масштаб — в асеті (GDD «Властивості гравця»)
    [SerializeField] private Collider2D walkArea;           // PolygonCollider2D (Is Trigger) по підлозі сцени

    [Header("Посилання")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;             // знадобиться в задачі 19 (параметр Speed)

    private Camera cam;
    private PlayerInteractor interactor;
    private Vector2 target;
    private bool isMoving;

    public bool CanMove { get; set; } = true;               // вимкнути під час діалогу, листа, пазла

    private void Awake()
    {
        cam = Camera.main;
        interactor = GetComponent<PlayerInteractor>();
        target = transform.position;
    }

    private void Update()
    {
        HandleClick();
        Move();
        ApplyDepthScale();
    }

    private void HandleClick()
    {
        if (!CanMove || Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // клік по кнопці HUD не має рухати Лілю
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 click = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // клік по предмету обробляє PlayerInteractor (задача 12)
        if (Interactable.At(click) != null) return;

        // клік по землі скасовує похід до предмета
        if (interactor != null) interactor.CancelPending();

        // клік поза підлогою -> найближча точка на краю WalkArea
        target = walkArea.OverlapPoint(click) ? click : walkArea.ClosestPoint(click);
        isMoving = true;
    }

    private void Move()
    {
        if (!isMoving)
        {
            SetAnimSpeed(0f);
            return;
        }

        Vector2 pos = transform.position;
        Vector2 next = Vector2.MoveTowards(pos, target, data.speed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        // розвернути спрайт у бік руху
        float dx = target.x - pos.x;
        if (Mathf.Abs(dx) > 0.01f) spriteRenderer.flipX = dx < 0f;

        if (Vector2.Distance(next, target) < 0.01f) isMoving = false;
        SetAnimSpeed(isMoving ? data.speed : 0f);
    }

    private void ApplyDepthScale()
    {
        // t = 0 внизу (nearY), t = 1 вгорі (farY)
        float t = Mathf.InverseLerp(data.nearY, data.farY, transform.position.y);
        float s = Mathf.Lerp(data.nearScale, data.farScale, t);
        transform.localScale = new Vector3(s, s, 1f);
    }

    private void SetAnimSpeed(float value)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat("Speed", value);
    }

    // задача 12: підійти до предмета, потім узяти
    public void WalkTo(Vector2 point)
    {
        target = walkArea.ClosestPoint(point);
        isMoving = true;
    }

    public bool IsMoving => isMoving;

    public void Stop()
    {
        isMoving = false;
        target = transform.position;
    }
}
