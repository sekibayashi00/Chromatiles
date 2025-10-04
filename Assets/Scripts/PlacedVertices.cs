using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlacedVertices : MonoBehaviour
{
    public static PlacedVertices Instance { get; private set; }

    private List<TileShape> shapes = new List<TileShape>(); // store all shape data of placed tiles

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
    void OnDestroy() { if (Instance == this) Instance = null; } // Ensure only one instance exists across scene progression between levels

    // Update is called once per frame
    void Update()
    {
        shapes.RemoveAll(shape => shape == null);

        foreach (TileShape shape in shapes)
        {
            for (int i = 0; i < shape.vertTransforms.Count; i++)
            {
                Vector3 v = shape.vertTransforms[i].position;
                Vector3 v3 = new Vector3(v.x, 0, v.z);
                Debug.DrawLine(v3, v3 + Vector3.up, Color.yellow); // draw line to show vertex position

                // draw line show outline of shape
                if (i != 0)
                {
                    Vector3 vPrev = shape.vertTransforms[i - 1].position;
                    vPrev = new Vector3(vPrev.x, 0, vPrev.z);
                    // Debug.DrawLine(v3 + Vector3.up * 0.5f, vPrev + Vector3.up * 0.5f, Color.cyan);
                }
                else
                {
                    Vector3 vPrev = shape.vertTransforms[shape.vertTransforms.Count - 1].position;
                    vPrev = new Vector3(vPrev.x, 0, vPrev.z);
                    // Debug.DrawLine(v3 + Vector3.up * 0.5f, vPrev + Vector3.up * 0.5f, Color.cyan);
                }
            }
        }
    }

    // public void AddVerts (List<Vector2> newVerts)
    // {
    //     foreach(Vector2 v in newVerts)
    //     {
    //         vertices.Add(v); // add new vertices to list
    //     }
    // }

    public void AddShape(TileShape shape)
    {
        shapes.Add(shape);
    }

    public void RemoveShape(TileShape shape)
    {
        shapes.Remove(shape);
    }

    // Since this component is instanced, clear the list of shapes whenever we change scene to avoid null references
    private void OnSceneLoaded(Scene sc, LoadSceneMode m)
    {
        shapes.Clear();
    }
}
