using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMarkerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum MarkerType { Start, Goal }
    public MarkerType type;

    private RectTransform rect;
    private Canvas canvas;
    private GridManager gridManager;
    private Image markerImage;

    private Vector2 dockPos;
    private bool isManuallyDragging = false;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        gridManager = FindFirstObjectByType<GridManager>();
        markerImage = GetComponent<Image>();
        
        dockPos = rect.anchoredPosition;
    }

    void Update()
    {
        if (isManuallyDragging && gridManager != null && gridManager.IsPathfindingRunning)
        {
            isManuallyDragging = false;
            ResetToDock();
            return;
        }

        if (isManuallyDragging)
        {
            FollowMousePosition();

            if (Input.GetMouseButtonUp(0))
            {
                isManuallyDragging = false;
                HandleDropPlacement();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (gridManager != null && gridManager.IsPathfindingRunning)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (gridManager != null) gridManager.IsDraggingMarker = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gridManager != null && gridManager.IsPathfindingRunning) return;
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (gridManager != null && gridManager.IsPathfindingRunning) return;
        HandleDropPlacement();
    }

    public void StartManualWorldDrag()
    {
        if (gridManager != null && gridManager.IsPathfindingRunning)
            return;

        if (gridManager != null)
            gridManager.IsDraggingMarker = true;

        isManuallyDragging = true;

        SetMarkerState(true);

        // Snap marker directly to cursor immediately
        FollowMousePosition();
    }

    public void ResetToDock()
    {
        isManuallyDragging = false;
        rect.anchoredPosition = dockPos;
        SetMarkerState(true);
        if (gridManager != null) gridManager.IsDraggingMarker = false;
    }

    private void FollowMousePosition()
    {
        Vector2 localCanvasPos;
        RectTransform parentRect = transform.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            Input.mousePosition,
            canvas.worldCamera,
            out localCanvasPos))
        {
            rect.anchoredPosition = localCanvasPos;
        }
    }

    private void HandleDropPlacement()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Node closest = gridManager.GetClosestNode(worldPos);

        if (closest != null && Vector2.Distance(worldPos, closest.worldPos) < gridManager.CellSize * 0.75f)
        {
            if (type == MarkerType.Start)
                gridManager.SetStart(closest);
            else
                gridManager.SetGoal(closest);

            rect.anchoredPosition = dockPos;
            SetMarkerState(false);
        }
        else
        {
            rect.anchoredPosition = dockPos;
            SetMarkerState(true);
        }

        if (gridManager != null) gridManager.IsDraggingMarker = false;
    }

    private void SetMarkerState(bool active)
    {
        if (markerImage != null)
        {
            markerImage.enabled = active;
            markerImage.raycastTarget = active; 
        }
    }
}