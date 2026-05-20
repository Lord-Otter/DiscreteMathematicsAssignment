using UnityEngine;

public enum TerrainType
{
    Wall   = 0,
    Mud    = 1,
    Road   = 2,
    Normal = 3
}

public static class TerrainSettings
{
    public static float GetCost(TerrainType type)
    {
        return type switch
        {
            TerrainType.Normal => 1.0f,
            TerrainType.Mud    => 4.0f,
            TerrainType.Road   => 0.3f,
            _                  => 1.0f
        };
    }

    public static Color GetColor(TerrainType type)
    {
        return type switch
        {
            TerrainType.Normal => new Color(0.94f, 0.96f, 0.98f),
            TerrainType.Mud    => new Color(0.55f, 0.27f, 0.07f),
            TerrainType.Road   => new Color(0.392f, 0.427f, 0.502f),
            TerrainType.Wall   => new Color(0.2f, 0.2f, 0.2f),
            _                  => Color.white
        };
    }
}

public class Node
{
    public Vector2Int gridPos;
    public Vector2 worldPos;

    public bool walkable = true;
    public float cost = 1f;
    public TerrainType terrainType = TerrainType.Normal;

    public float gCost; 
    public float hCost;
    public float fCost => gCost + hCost; 

    public Node previous;
    public GameObject visual;

    public bool isStart;
    public bool isGoal;

    public Node(Vector2Int gridPos, Vector2 worldPos)
    {
        this.gridPos = gridPos;
        this.worldPos = worldPos;
        Reset();
    }

    public void Reset()
    {
        gCost = float.PositiveInfinity;
        hCost = 0f;
        previous = null;
    }

    public void ClearState()
    {
        isStart = false;
        isGoal = false;
        SetTerrain(TerrainType.Normal);
    }

    public void SetTerrain(TerrainType newType)
    {
        terrainType = newType;
        
        walkable = (newType != TerrainType.Wall);
        cost = TerrainSettings.GetCost(newType);

        ForceResetVisual();
    }

    public void SetColor(Color overlayColor)
    {
        if (visual == null) return;

        if (overlayColor == NodeColors.ActiveFrontier || 
            overlayColor == NodeColors.Start || 
            overlayColor == NodeColors.Goal)
        {
            overlayColor.a = 1f;
            visual.GetComponent<SpriteRenderer>().color = overlayColor;
            return;
        }

        if (overlayColor == TerrainSettings.GetColor(terrainType))
        {
            visual.GetComponent<SpriteRenderer>().color = overlayColor;
            return;
        }

        Color terrainColor = TerrainSettings.GetColor(terrainType);
        float blendRatio = overlayColor.a < 1f ? overlayColor.a : 0.4f; 
        
        Color blendedColor = Color.Lerp(terrainColor, overlayColor, blendRatio);
        blendedColor.a = 1f;

        visual.GetComponent<SpriteRenderer>().color = blendedColor;
    }

    public void ForceResetVisual()
    {
        if (visual == null) return;

        var sr = visual.GetComponent<SpriteRenderer>();
        sr.color = TerrainSettings.GetColor(terrainType);
    }
}