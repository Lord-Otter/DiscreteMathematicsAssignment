using UnityEngine;

public class DraggableMarker : MonoBehaviour
{
    public enum MarkerType { Start, Goal }
    public MarkerType type;

    private enum State
    {
        Docked,
        Dragging,
        Placed,
        Returning
    }

    private State state;

    [Header("Docking")]
    public Vector3 cameraLocalOffset = new Vector3(1f, 1f, 2f);

    private Camera cam;
    private Transform camTransform;
    private GridManager gridManager;

    private Vector3 returnStartPos;

    private SpriteRenderer sr;

    void Awake()
    {
        cam = Camera.main;
        camTransform = cam.transform;
        gridManager = FindFirstObjectByType<GridManager>();

        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        DockToCameraInstant();
    }

    void Update()
    {
        switch (state)
        {
            case State.Docked:
                break;

            case State.Dragging:
                FollowMouse();
                break;

            case State.Returning:
                AnimateReturn();
                break;

            case State.Placed:
                break;
        }
    }

   void OnMouseDown()
    {
        if (gridManager != null)
        {
            gridManager.IsDraggingMarker = true;

            if (state == State.Placed)
            {
                sr.enabled = true;
                if (type == MarkerType.Start)
                    gridManager.ClearStart();
                else
                    gridManager.ClearGoal();
            }
        }

        state = State.Dragging;
        transform.SetParent(null);
    }

    void OnMouseUp()
    {
        if (gridManager != null)
        {
            gridManager.IsDraggingMarker = false;
        }

        Node closest = gridManager.GetClosestNode(transform.position);

        if (closest != null)
        {
            float dist = Vector2.Distance(transform.position, closest.worldPos);

            if (dist < gridManager.CellSize * 0.75f)
            {
                PlaceOnNode(closest);
                return;
            }
        }

        if (type == MarkerType.Start)
            gridManager.ClearStart();
        else
            gridManager.ClearGoal();

        BeginReturn();
    }

    void FollowMouse()
    {
        Vector2 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        transform.position = mouse;
    }

    void PlaceOnNode(Node node)
    {
        state = State.Placed;

        transform.position = node.worldPos;
        transform.SetParent(null);

        sr.enabled = false;

        if (type == MarkerType.Start)
            gridManager.SetStart(node);
        else
            gridManager.SetGoal(node);
    }

    void BeginReturn()
    {
        state = State.Returning;

        sr.enabled = true;

        returnStartPos = transform.position;

        DockToCameraInstant();
    }

    void AnimateReturn()
    {
        Vector3 target = camTransform.TransformPoint(cameraLocalOffset);

        float t = 1f - Mathf.Exp(-Time.deltaTime * 8f);

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            t
        );

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            DockToCameraInstant();
        }
    }

    void DockToCameraInstant()
    {
        state = State.Docked;

        transform.SetParent(camTransform);
        transform.localPosition = cameraLocalOffset;
    }

    public void ResetToDock()
    {
        sr.enabled = true;
        state = State.Docked;
        DockToCameraInstant();
    }
}