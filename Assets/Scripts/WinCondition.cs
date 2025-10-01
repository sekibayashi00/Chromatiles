using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WinCondition : MonoBehaviour
{
    public static WinCondition Instance { get; private set; }

    [Header("Win Condition Settings")]
    public bool checkOutline = true;
    public bool checkColors = true;
    public float checkDelay = 0.5f; // Delay before checking after tile placement
    
    [Header("Outline Matching")]
    public List<Vector2> targetOutlinePoints = new List<Vector2>();
    public float outlineMatchThreshold = 0.1f;
    [Range(0f, 1f)]
    public float requiredOutlineMatchPercentage = 0.8f; // 80% of points must match
    
    [Header("Color Matching")]
    public List<ColorTargetRegion> colorTargets = new List<ColorTargetRegion>();
    public float colorMatchTolerance = 0.15f;
    
    [Header("Debug")]
    public bool showDebugVisuals = true;
    public bool logDetailedInfo = false;
    
    [Header("Events")]
    public UnityEvent onWinConditionMet;
    public UnityEvent onWinConditionFailed;
    
    private bool isChecking = false;
    private bool lastCheckResult = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Update()
    {
        if (showDebugVisuals)
        {
            DrawDebugVisuals();
        }

        // Manual check with Space key for testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Manual win condition check triggered");
            CheckWinConditionImmediate();
        }
    }

    /// <summary>
    /// Checks win condition with a delay. Call this when tiles are placed.
    /// </summary>
    public void CheckWinConditionDelayed()
    {
        if (!isChecking)
        {
            StartCoroutine(CheckWithDelay());
        }
    }

    /// <summary>
    /// Checks win condition immediately without delay
    /// </summary>
    public bool CheckWinConditionImmediate()
    {
        bool outlineMatch = true;
        bool colorMatch = true;

        if (checkOutline)
        {
            outlineMatch = CheckOutlineMatching();
            if (logDetailedInfo)
            {
                Debug.Log($"Outline Match: {outlineMatch}");
            }
        }

        if (checkColors)
        {
            colorMatch = CheckColorMatching();
            if (logDetailedInfo)
            {
                Debug.Log($"Color Match: {colorMatch}");
            }
        }

        bool won = outlineMatch && colorMatch;
        lastCheckResult = won;

        if (won)
        {
            Debug.Log("🎉 WIN CONDITION MET!");
            onWinConditionMet?.Invoke();
        }
        else
        {
            if (logDetailedInfo)
            {
                Debug.Log("Win condition not met yet");
            }
            onWinConditionFailed?.Invoke();
        }

        return won;
    }

    private IEnumerator CheckWithDelay()
    {
        isChecking = true;
        yield return new WaitForSeconds(checkDelay);
        CheckWinConditionImmediate();
        isChecking = false;
    }

    private bool CheckOutlineMatching()
    {
        if (targetOutlinePoints == null || targetOutlinePoints.Count == 0)
        {
            Debug.LogWarning("No target outline points defined!");
            return false;
        }

        // Get all placed tile vertices
        List<Vector2> placedVertices = GetAllPlacedTileVertices();

        if (placedVertices.Count == 0)
        {
            if (logDetailedInfo)
            {
                Debug.Log("No tiles placed yet");
            }
            return false;
        }

        // Check how many target points have a matching placed vertex nearby
        int matchedPoints = 0;
        int requiredMatches = Mathf.CeilToInt(targetOutlinePoints.Count * requiredOutlineMatchPercentage);

        foreach (Vector2 targetPoint in targetOutlinePoints)
        {
            bool foundMatch = false;
            float closestDistance = float.MaxValue;

            foreach (Vector2 placedVertex in placedVertices)
            {
                float distance = Vector2.Distance(targetPoint, placedVertex);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }

                if (distance <= outlineMatchThreshold)
                {
                    foundMatch = true;
                    break;
                }
            }

            if (foundMatch)
            {
                matchedPoints++;
            }
            else if (logDetailedInfo)
            {
                Debug.Log($"Target point {targetPoint} not matched (closest: {closestDistance:F3})");
            }
        }

        bool outlineMatches = matchedPoints >= requiredMatches;

        if (logDetailedInfo)
        {
            Debug.Log($"Outline Match: {matchedPoints}/{targetOutlinePoints.Count} points matched (need {requiredMatches})");
        }

        return outlineMatches;
    }

    private bool CheckColorMatching()
    {
        if (colorTargets == null || colorTargets.Count == 0)
        {
            // No color targets means this check passes
            return true;
        }

        foreach (ColorTargetRegion target in colorTargets)
        {
            Color actualColor = SampleColorAtPosition(target.position);
            bool matches = CompareColors(actualColor, target.targetColor, colorMatchTolerance);

            if (!matches)
            {
                if (logDetailedInfo)
                {
                    Debug.Log($"Color mismatch at {target.position}: Expected {target.targetColor}, Got {actualColor}");
                }
                return false;
            }
        }

        return true;
    }

    private List<Vector2> GetAllPlacedTileVertices()
    {
        List<Vector2> vertices = new List<Vector2>();
        
        // Find all tiles in the scene
        TileVertices[] allTiles = FindObjectsOfType<TileVertices>();

        foreach (TileVertices tile in allTiles)
        {
            // Only count tiles that are not currently being grabbed
            if (!tile.isGrabbed)
            {
                TileShape shape = tile.GetComponent<TileShape>();
                if (shape != null && shape.vertTransforms != null)
                {
                    foreach (Transform vertTransform in shape.vertTransforms)
                    {
                        if (vertTransform != null)
                        {
                            // Convert 3D position to 2D (X, Z plane)
                            Vector2 vertex2D = new Vector2(
                                vertTransform.position.x, 
                                vertTransform.position.z
                            );
                            vertices.Add(vertex2D);
                        }
                    }
                }
            }
        }

        return vertices;
    }

    private Color SampleColorAtPosition(Vector2 position)
    {
        Color combinedColor = Color.black;
        TileVertices[] allTiles = FindObjectsOfType<TileVertices>();

        foreach (TileVertices tile in allTiles)
        {
            if (!tile.isGrabbed)
            {
                // Check if the position is inside this tile's polygon
                if (IsPointInsideTilePolygon(position, tile))
                {
                    // Get the tile's color from its material
                    MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        // Try to get color from common shader properties
                        if (renderer.material.HasProperty("_BaseColor"))
                        {
                            Color tileColor = renderer.material.GetColor("_BaseColor");
                            // Additive color mixing
                            combinedColor.r += tileColor.r;
                            combinedColor.g += tileColor.g;
                            combinedColor.b += tileColor.b;
                        }
                        else if (renderer.material.HasProperty("_Color"))
                        {
                            Color tileColor = renderer.material.GetColor("_Color");
                            combinedColor.r += tileColor.r;
                            combinedColor.g += tileColor.g;
                            combinedColor.b += tileColor.b;
                        }
                    }
                }
            }
        }

        // Clamp values to 0-1 range
        combinedColor.r = Mathf.Clamp01(combinedColor.r);
        combinedColor.g = Mathf.Clamp01(combinedColor.g);
        combinedColor.b = Mathf.Clamp01(combinedColor.b);
        combinedColor.a = 1.0f;

        return combinedColor;
    }

    private bool IsPointInsideTilePolygon(Vector2 point, TileVertices tile)
    {
        TileShape shape = tile.GetComponent<TileShape>();
        if (shape == null || shape.vertTransforms == null || shape.vertTransforms.Count < 3)
        {
            return false;
        }

        // Build polygon from vertex transforms
        List<Vector2> polygon = new List<Vector2>();
        foreach (Transform vertTransform in shape.vertTransforms)
        {
            if (vertTransform != null)
            {
                polygon.Add(new Vector2(vertTransform.position.x, vertTransform.position.z));
            }
        }

        return IsPointInPolygon(point, polygon);
    }

    private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        if (polygon.Count < 3) return false;

        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / 
                (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private bool CompareColors(Color a, Color b, float tolerance)
    {
        return Mathf.Abs(a.r - b.r) <= tolerance &&
               Mathf.Abs(a.g - b.g) <= tolerance &&
               Mathf.Abs(a.b - b.b) <= tolerance;
    }

    private void DrawDebugVisuals()
    {
        // Draw target outline
        if (targetOutlinePoints != null && targetOutlinePoints.Count >= 2)
        {
            Color lineColor = lastCheckResult ? Color.green : Color.white;

            for (int i = 0; i < targetOutlinePoints.Count; i++)
            {
                Vector2 current = targetOutlinePoints[i];
                Vector2 next = targetOutlinePoints[(i + 1) % targetOutlinePoints.Count];

                Vector3 start = new Vector3(current.x, 0.05f, current.y);
                Vector3 end = new Vector3(next.x, 0.05f, next.y);

                Debug.DrawLine(start, end, lineColor);
            }

            // Draw points
            foreach (Vector2 point in targetOutlinePoints)
            {
                Vector3 pos = new Vector3(point.x, 0.05f, point.y);
                Debug.DrawLine(pos, pos + Vector3.up * 0.2f, Color.yellow);
            }
        }

        // Draw color target positions
        if (colorTargets != null)
        {
            foreach (ColorTargetRegion target in colorTargets)
            {
                Vector3 pos = new Vector3(target.position.x, 0.1f, target.position.y);
                Debug.DrawLine(pos, pos + Vector3.up * 0.3f, target.targetColor);
                
                // Draw a small cross
                Debug.DrawLine(pos + Vector3.left * 0.1f, pos + Vector3.right * 0.1f, target.targetColor);
                Debug.DrawLine(pos + Vector3.forward * 0.1f, pos + Vector3.back * 0.1f, target.targetColor);
            }
        }
    }

    // Public helper methods
    public void SetTargetOutline(List<Vector2> points)
    {
        targetOutlinePoints = new List<Vector2>(points);
    }

    public void AddColorTarget(Vector2 position, Color targetColor)
    {
        colorTargets.Add(new ColorTargetRegion
        {
            position = position,
            targetColor = targetColor
        });
    }

    public void ClearColorTargets()
    {
        colorTargets.Clear();
    }

    public void ClearOutline()
    {
        targetOutlinePoints.Clear();
    }
}

[System.Serializable]
public class ColorTargetRegion
{
    public Vector2 position;
    public Color targetColor = Color.white;
    
    [Tooltip("Optional name for this color region")]
    public string regionName;
}