using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class GridManager : MonoBehaviour
{
    private Pathfinder pathfinder;
    private Node startNode;
    private Node goalNode;
    private Coroutine runningPathRoutine;

    [Header("Grid")]
    public float gridSize = 10f;
    [Range(1, 100)]
    public int resolution = 10;

    [Header("Prefab")]
    public GameObject nodePrefab;

    [Header("UI")]
    public TMP_InputField resolutionInput;
    public TMP_Dropdown brushDropdown;
    public TMP_Dropdown algorithmDropdown;
    public Button runButton; 
    public TextMeshProUGUI statusText;
    private TextMeshProUGUI runButtonText;
    public Button applyResolutionButton;
    public Selectable[] disableWhileRunning;

    [Header("References")]
    public UIMarkerDrag startMarker;
    public UIMarkerDrag goalMarker;

    [Header("Terrain Editor Settings")]
    public TerrainType activePaintingType = TerrainType.Wall;
    private Camera mainCam;
    public bool IsDraggingMarker { get; set; }

    [HideInInspector]
    public PathfindingAlgorithm activeAlgorithm = PathfindingAlgorithm.AStar;

    public Node[,] grid;
    public float CellSize => gridSize / resolution;

    public bool IsPathfindingRunning => runningPathRoutine != null;

    void Awake()
    {
        mainCam = Camera.main;
        if (runButton != null)
        {
            runButtonText = runButton.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        if (runButton != null)
            runButton.onClick.AddListener(TogglePathfindingExecution);

        if (brushDropdown != null)
        {
            brushDropdown.onValueChanged.RemoveListener(ChangePaintingTerrainType);
            activePaintingType = TerrainType.Wall;
            brushDropdown.value = (int)activePaintingType;
            brushDropdown.RefreshShownValue();
            brushDropdown.onValueChanged.AddListener(ChangePaintingTerrainType);
        }

        if (algorithmDropdown != null)
        {
            algorithmDropdown.onValueChanged.RemoveListener(ChangePathfindingAlgorithm);
            activeAlgorithm = PathfindingAlgorithm.AStar;
            algorithmDropdown.value = (int)activeAlgorithm;
            algorithmDropdown.RefreshShownValue();
            algorithmDropdown.onValueChanged.AddListener(ChangePathfindingAlgorithm);
        }

        grid = null;
        pathfinder = null;
        runningPathRoutine = null;

        RegenerateGrid();
    }

    void Update()
    {
        if (IsPathfindingRunning) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleGridMarkerPickup();
        }

        if (!IsDraggingMarker)
        {
            HandleTerrainPainting();
        }
    }

    void HandleTerrainPainting()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool leftClickHeld = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);

        if (leftClickHeld || rightClick)
        {
            Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            
            float cellSize = CellSize;
            Vector2 origin = new Vector2(
                -((resolution - 1) * cellSize) * 0.5f,
                -((resolution - 1) * cellSize) * 0.5f
            );

            int x = Mathf.RoundToInt((mouseWorldPos.x - origin.x) / cellSize);
            int y = Mathf.RoundToInt((mouseWorldPos.y - origin.y) / cellSize);

            if (x >= 0 && x < resolution && y >= 0 && y < resolution)
            {
                Node targetNode = grid[x, y];
                float dist = Vector2.Distance(mouseWorldPos, targetNode.worldPos);

                if (dist < cellSize * 0.5f)
                {
                    if (!targetNode.isStart && !targetNode.isGoal)
                    {
                        TerrainType typeToPaint = leftClickHeld ? activePaintingType : TerrainType.Normal;
                        
                        targetNode.SetTerrain(typeToPaint);
                        ClearPathVisualsOnly();
                        UpdateUIState();
                    }
                }
            }
        }
    }

    void HandleGridMarkerPickup()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        float cellSize = CellSize;
        Vector2 origin = new Vector2(
            -((resolution - 1) * cellSize) * 0.5f,
            -((resolution - 1) * cellSize) * 0.5f
        );

        int x = Mathf.RoundToInt((mouseWorldPos.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((mouseWorldPos.y - origin.y) / cellSize);

        if (x >= 0 && x < resolution && y >= 0 && y < resolution)
        {
            Node targetNode = grid[x, y];
            float dist = Vector2.Distance(mouseWorldPos, targetNode.worldPos);

            if (dist < cellSize * 0.5f)
            {
                if (targetNode.isStart && startMarker != null)
                {
                    ClearStartOnlyData();
                    ClearPathVisualsOnly();
                    UpdateUIState();
                    
                    startMarker.StartManualWorldDrag();
                }
                else if (targetNode.isGoal && goalMarker != null)
                {
                    ClearGoalOnlyData();
                    ClearPathVisualsOnly();
                    UpdateUIState();
                    
                    goalMarker.StartManualWorldDrag();
                }
            }
        }
    }

    public void ApplyResolution()
    {
        if (IsPathfindingRunning) return;

        if (int.TryParse(resolutionInput.text, out int newResolution))
        {
            resolution = Mathf.Max(2, newResolution);
            RegenerateGrid();
        }
    }

    void GenerateGrid()
    {
        grid = new Node[resolution, resolution];
        float cellSize = CellSize;

        Vector2 origin = new Vector2(
            -((resolution - 1) * cellSize) * 0.5f,
            -((resolution - 1) * cellSize) * 0.5f
        );

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                Vector2 pos = origin + new Vector2(x * cellSize, y * cellSize);

                GameObject obj = Instantiate(nodePrefab, pos, Quaternion.identity);
                obj.transform.localScale = Vector3.one * (cellSize * 0.95f);

                Node node = new Node(new Vector2Int(x, y), pos);
                node.visual = obj;
                node.SetTerrain(TerrainType.Normal);

                grid[x, y] = node;
            }
        }
    }

    void RegenerateGrid()
    {
        StopActivePathfinding();

        ClearGrid();
        GenerateGrid();

        pathfinder = new Pathfinder(grid);

        startNode = null;
        goalNode = null;

        if (startMarker != null) startMarker.ResetToDock();
        if (goalMarker != null) goalMarker.ResetToDock();

        UpdateUIState();
    }

    void ClearGrid()
    {
        if (grid == null) return;
        foreach (var n in grid)
        {
            if (n != null && n.visual != null) Destroy(n.visual);
        }
    }

    public Node GetClosestNode(Vector2 worldPos)
    {
        Node best = null;
        float bestDist = float.MaxValue;
        foreach (var n in grid)
        {
            float d = Vector2.Distance(worldPos, n.worldPos);
            if (d < bestDist) { bestDist = d; best = n; }
        }
        return best;
    }

    public void SetStart(Node node)
    {
        if (startNode != null)
        {
            startNode.isStart = false;
            Node oldStart = startNode;
            startNode = null; 
            ResetNodeVisual(oldStart);
        }
        startNode = node;
        startNode.isStart = true;
        ResetNodeVisual(startNode); 
        UpdateUIState();
    }

    public void SetGoal(Node node)
    {
        if (goalNode != null)
        {
            goalNode.isGoal = false;
            Node oldGoal = goalNode;
            goalNode = null; 
            ResetNodeVisual(oldGoal);
        }
        goalNode = node;
        goalNode.isGoal = true;
        ResetNodeVisual(goalNode); 
        UpdateUIState();
    }

    public void ClearStart()
    {
        StopActivePathfinding(); 
        ClearStartOnlyData();
        if (startMarker != null) startMarker.ResetToDock();
        ClearPathVisualsOnly();
        UpdateUIState();
    }

    public void ClearGoal()
    {
        StopActivePathfinding(); 
        ClearGoalOnlyData();
        if (goalMarker != null) goalMarker.ResetToDock();
        ClearPathVisualsOnly();
        UpdateUIState();
    }

    public void ClearStartOnlyData()
    {
        if (startNode != null)
        {
            startNode.isStart = false;
            ResetNodeVisual(startNode);
            startNode = null;
        }
    }

    public void ClearGoalOnlyData()
    {
        if (goalNode != null)
        {
            goalNode.isGoal = false;
            ResetNodeVisual(goalNode);
            goalNode = null;
        }
    }

    private void TogglePathfindingExecution()
    {
        if (IsPathfindingRunning)
        {
            StopActivePathfinding();
        }
        else
        {
            RunPath();
        }
    }

    public void RunPath()
    {
        if (startNode == null || goalNode == null) return;

        ClearPathVisualsOnly();

        runningPathRoutine = StartCoroutine(PathfindingWrapperRoutine());

        // IMPORTANT
        UpdateUIState();
    }

    private IEnumerator PathfindingWrapperRoutine()
    {
        SetButtonToStopMode();

        yield return StartCoroutine(
            pathfinder.FindPathVisual(startNode.gridPos, goalNode.gridPos, activeAlgorithm)
        );

        runningPathRoutine = null;

        UpdateUIState();
    }

    private void StopActivePathfinding()
    {
        if (grid == null) return;

        if (runningPathRoutine != null)
        {
            StopCoroutine(runningPathRoutine);
            runningPathRoutine = null;
        }

        if (pathfinder != null)
        {
            pathfinder.ForceKillExecution();
        }

        foreach (var n in grid)
        {
            if (n == null) continue;

            n.gCost = float.PositiveInfinity;
            n.previous = null;

            if (n.isStart)
            {
                n.SetColor(NodeColors.Start);
                continue;
            }

            if (n.isGoal)
            {
                n.SetColor(NodeColors.Goal);
                continue;
            }

            n.ForceResetVisual();
        }

        UpdateUIState();
    }

    void ClearPathVisualsOnly()
    {
        if (grid == null) return;

        foreach (var n in grid)
        {
            if (n == null) continue;

            n.gCost = float.PositiveInfinity;
            n.previous = null;

            if (n.isStart)
            {
                n.SetColor(NodeColors.Start);
                continue;
            }

            if (n.isGoal)
            {
                n.SetColor(NodeColors.Goal);
                continue;
            }

            n.ForceResetVisual();
        }
    }

    void ResetNodeVisual(Node n)
    {
        if (n == null) return;

        if (n.isStart) { n.SetColor(NodeColors.Start); return; }
        if (n.isGoal) { n.SetColor(NodeColors.Goal); return; }

        n.SetColor(TerrainSettings.GetColor(n.terrainType));
    }

    private void UpdateUIState()
    {
        bool hasStart = startNode != null;
        bool hasGoal = goalNode != null;
        bool running = IsPathfindingRunning;

        // ---------------------------
        // RUN BUTTON
        // ---------------------------
        if (runButton != null)
        {
            runButton.interactable = running || (hasStart && hasGoal);

            ColorBlock cb = runButton.colors;

            if (!running)
            {
                cb.normalColor = Color.white;
                cb.selectedColor = Color.white;

                if (runButtonText != null)
                    runButtonText.text = "RUN!";
            }

            runButton.colors = cb;
        }

        // ---------------------------
        // LOCK UI DURING PATHFINDING
        // ---------------------------
        foreach (Selectable ui in disableWhileRunning)
        {
            if (ui == null)
                continue;

            // Force-close dropdowns while running
            TMP_Dropdown tmpDropdown = ui as TMP_Dropdown;

            if (running && tmpDropdown != null)
            {
                tmpDropdown.Hide();
            }

            ui.interactable = !running;
        }

        // ---------------------------
        // STATUS TEXT
        // ---------------------------
        if (statusText != null)
        {
            if (running)
            {
                statusText.text = "Finding optimal path...";
                statusText.color = Color.yellow;
            }
            else if (hasStart && hasGoal)
            {
                statusText.text = "Ready to run!";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Place Start/Goal marker";
                statusText.color = Color.red;
            }
        }
    }

    private void SetButtonToStopMode()
    {
        if (runButton == null) return;

        ColorBlock cb = runButton.colors;

        Color stopRed = new Color(0.85f, 0.2f, 0.2f, 1f);

        cb.normalColor = stopRed;
        cb.selectedColor = stopRed;

        runButton.colors = cb;

        if (runButtonText != null)
            runButtonText.text = "STOP";
    }

    public void ChangePaintingTerrainType(int typeIndex)
    {
        if (IsPathfindingRunning) return;

        ClearPathVisualsOnly();
        activePaintingType = (TerrainType)typeIndex;
        Debug.Log($"Brush changed to: {activePaintingType}");
    }

    public void ChangePathfindingAlgorithm(int index)
    {
        if (IsPathfindingRunning) return;

        ClearPathVisualsOnly();
        activeAlgorithm = (PathfindingAlgorithm)index;
        Debug.Log($"Algorithm mode swapped to: {activeAlgorithm}");
    }
}