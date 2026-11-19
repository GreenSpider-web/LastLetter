using UnityEngine;

// Розтягує фон на всю висоту кадру і прикладає його лівим краєм до лівого краю камери.
// Чим ширше вікно гри, тим більше фону видно праворуч — без чорних смуг (фон 3:1).
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFit : MonoBehaviour
{
    private SpriteRenderer sr;
    private Camera cam;
    private float lastAspect = -1f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }

    // перевіряємо щокадру, бо вікно гри можуть розтягнути
    private void LateUpdate()
    {
        if (cam == null || sr.sprite == null || Mathf.Approximately(cam.aspect, lastAspect)) return;
        lastAspect = cam.aspect;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector2 size = sr.sprite.bounds.size;

        // за висотою; якщо вікно ще ширше за фон — збільшити, щоб закрити й ширину
        float scale = Mathf.Max(camHeight / size.y, camWidth / size.x);
        transform.localScale = new Vector3(scale, scale, 1f);

        float camLeft = cam.transform.position.x - camWidth / 2f;
        transform.position = new Vector3(camLeft + size.x * scale / 2f, cam.transform.position.y, transform.position.z);
    }
}
