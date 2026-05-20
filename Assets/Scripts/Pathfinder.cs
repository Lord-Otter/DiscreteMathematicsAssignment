using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathfindingAlgorithm
{
    AStar,
    Dijkstra
}

public class Pathfinder
{
    private Node[,] grid;
    private int width;
    private int height;
    private bool forceTermination = false;
    
    private PathfindingAlgorithm activeAlgorithm = PathfindingAlgorithm.AStar;

    private Vector2Int[] dirs =
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    public Pathfinder(Node[,] grid)
    {
        this.grid = grid;
        width = grid.GetLength(0);
        height = grid.GetLength(1);
    }

    public void ForceKillExecution()
    {
        forceTermination = true;
    }

    private float GetHeuristic(Vector2Int a, Vector2Int b)
    {
        if (activeAlgorithm == PathfindingAlgorithm.Dijkstra)
        {
            return 0f; 
        }
        
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public IEnumerator FindPathVisual(Vector2Int start, Vector2Int goal, PathfindingAlgorithm algorithm)
    {
        forceTermination = false;
        activeAlgorithm = algorithm;

        Node startNode = grid[start.x, start.y];
        Node goalNode = grid[goal.x, goal.y];

        foreach (var n in grid)
        {
            n.Reset();

            if (n.isStart)
                n.SetColor(NodeColors.Start);
            else if (n.isGoal)
                n.SetColor(NodeColors.Goal);
            else
                n.SetColor(TerrainSettings.GetColor(n.terrainType));
        }

        List<Node> open = new List<Node>();

        startNode.gCost = 0;
        startNode.hCost = GetHeuristic(start, goal);
        open.Add(startNode);

        bool pathFound = false;

        while (open.Count > 0)
        {
            if (forceTermination) yield break;

            Node current = GetLowestFCost(open);
            open.Remove(current);

            if (!current.isStart && !current.isGoal)
                current.SetColor(NodeColors.ActiveFrontier);

            if (current == goalNode)
            {
                pathFound = true;
                break;
            }

            foreach (var n in GetNeighbors(current))
            {
                if (!n.walkable) continue;

                float newGCost = current.gCost + n.cost;

                if (newGCost < n.gCost)
                {
                    n.gCost = newGCost;
                    n.hCost = GetHeuristic(n.gridPos, goal);
                    n.previous = current;

                    if (!open.Contains(n))
                    {
                        open.Add(n);

                        if (!n.isStart && !n.isGoal)
                            n.SetColor(NodeColors.Frontier);
                    }
                }
            }

            yield return new WaitForSeconds(0.03f / SpeedController.SpeedMultiplier);

            if (forceTermination) yield break;

            if (!current.isStart && !current.isGoal)
                current.SetColor(NodeColors.Visited);
        }

        if (pathFound && !forceTermination)
        {
            List<Node> path = BuildPathList(goalNode);

            foreach (var n in path)
            {
                if (forceTermination) yield break;
                if (n.isStart || n.isGoal) continue;

                n.SetColor(NodeColors.Path);
                yield return new WaitForSeconds(0.01f);
            }
        }
    }

    List<Node> GetNeighbors(Node node)
    {
        List<Node> result = new List<Node>();

        foreach (var d in dirs)
        {
            int nx = node.gridPos.x + d.x;
            int ny = node.gridPos.y + d.y;

            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                continue;

            result.Add(grid[nx, ny]);
        }

        return result;
    }

    Node GetLowestFCost(List<Node> list)
    {
        Node best = list[0];

        foreach (var n in list)
        {
            if (n.fCost < best.fCost || (Mathf.Approximately(n.fCost, best.fCost) && n.hCost < best.hCost))
            {
                best = n;
            }
        }

        return best;
    }

    List<Node> BuildPathList(Node goal)
    {
        List<Node> path = new List<Node>();
        Node current = goal;

        while (current != null)
        {
            path.Add(current);
            current = current.previous;
        }
        return path;
    }
}