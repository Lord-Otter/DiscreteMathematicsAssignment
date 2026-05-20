using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private Camera cam;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minLogZoom = 2f;
    public float maxLogZoom = 20f;

    [Header("Pan Settings")]
    public int panMouseButton = 2; 

    [Header("Boundary Limits")]
    public Vector2 minBounds = new Vector2(-15f, -15f);
    public Vector2 maxBounds = new Vector2(15f, 15f);

    private Vector3 dragOrigin;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        HandleZoom();
        HandlePan();
        ConstrainPosition();
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            float currentZoom = cam.orthographicSize;
            currentZoom -= scrollInput * zoomSpeed * (currentZoom * 0.5f);
            
            cam.orthographicSize = Mathf.Clamp(currentZoom, minLogZoom, maxLogZoom);
        }
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(panMouseButton))
        {
            Vector3 currentMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = dragOrigin - currentMousePos;

            transform.position += difference;
        }
    }

    void ConstrainPosition()
    {
        Vector3 clampedPosition = transform.position;

        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBounds.x, maxBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minBounds.y, maxBounds.y);

        transform.position = clampedPosition;
    }
}