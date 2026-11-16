using UnityEngine;

// Камера плавно йде за Лілею по x і ніколи не показує нічого лівіше minX і правіше maxX.
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;          // Ліля
    [SerializeField] private float minX = -10f;         // лівий край фону сцени
    [SerializeField] private float maxX = 10f;          // правий край фону сцени
    [SerializeField] private float smoothTime = 0.25f;  // 0 = без згладжування

    private Camera cam;
    private float velocityX;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // на старті сцени одразу стати на місце, без «під'їзду»
        if (target != null)
            transform.position = new Vector3(ClampX(target.position.x), transform.position.y, transform.position.z);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float x = Mathf.SmoothDamp(transform.position.x, target.position.x, ref velocityX, smoothTime);
        transform.position = new Vector3(ClampX(x), transform.position.y, transform.position.z);
    }

    private float ClampX(float x)
    {
        // половина ширини того, що бачить камера
        float halfWidth = cam.orthographicSize * cam.aspect;
        float left = minX + halfWidth;
        float right = maxX - halfWidth;

        // фон вужчий за екран -> просто стати по центру
        if (left > right) return (minX + maxX) * 0.5f;

        return Mathf.Clamp(x, left, right);
    }

    // жовті лінії в Scene, щоб бачити межі
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minX, -20f, 0f), new Vector3(minX, 20f, 0f));
        Gizmos.DrawLine(new Vector3(maxX, -20f, 0f), new Vector3(maxX, 20f, 0f));
    }
}
