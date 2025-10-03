using System.Collections.Generic;
using UnityEngine;

public class TileVertices : MonoBehaviour
{
    //[SerializeField]
    //private List<Transform> vertTransforms = new List<Transform>();
    private TileShape shape;
    private PlacedVertices vertManager;
    public bool isGrabbed = false;
    public bool flippable = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        shape = GetComponent<TileShape>();

        //GetVertexManager();
        TilePlaced();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"Vertex Manager is {PlacedVertices.Instance}");

        // allow rotation with mouse wheel if grabbed
        if(isGrabbed)
        {
            //Debug.Log($"Mouse wheel axis: {Input.GetAxis("Mouse ScrollWheel")}");
            if (Input.GetAxis("Mouse ScrollWheel") == 0.1f)
            {
                //Debug.Log("Wheel up");
                transform.Rotate(0, -5.0f, 0);
            } else if (Input.GetAxis("Mouse ScrollWheel") == -0.1f)
            {
                //Debug.Log("Wheel down");
                transform.Rotate(0, 5.0f, 0);
            }

            if(Input.GetKeyDown(KeyCode.Mouse1))
            {
                //Debug.Log("Right mouse button clicked");
                if (flippable) transform.Rotate(180.0f, 0.0f, 0.0f); // flip tile
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        transform.position += Vector3.up * 0.2f;
    }

    void TileGrabbed()
    {
        PlacedVertices.Instance.RemoveShape(shape);
        MouseScript.Instance.GrabTile(transform);
    }

    public void TilePlaced()
    {
        PlacedVertices.Instance.AddShape(shape);
        MouseScript.Instance.DropTile();
    }

    private void OnMouseDown()
    {
        //Debug.Log("Mouse clicked!");
        TileGrabbed();
    }

    private void OnMouseUp()
    {
        //Debug.Log("Mouse released");
        TilePlaced();
    }
}
