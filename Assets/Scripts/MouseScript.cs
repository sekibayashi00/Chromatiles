using UnityEngine;
using UnityEngine.InputSystem;

public class MouseScript : MonoBehaviour
{
    public static MouseScript Instance { get; private set; }
    public Camera mainCamera;
    private bool holdingTile = false;
    private Transform heldTile = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        mainCamera = Camera.main;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1.0f));
        worldPos.y = 0.0f;
        transform.position = worldPos;

        if (holdingTile && heldTile != null)
        {
            heldTile.position = transform.position;
        }
    }

    public void GrabTile(Transform tile)
    {
        if (!holdingTile && heldTile == null)
        {
            holdingTile = true;
            heldTile = tile;
            heldTile.GetComponent<TileVertices>().isGrabbed = true;
            //tile.parent = transform;
        }
    }

    public void DropTile ()
    {
        if (holdingTile && heldTile != null)
        {
            holdingTile = false;
            heldTile.GetComponent<TileVertices>().isGrabbed = false;
            heldTile = null;
        }
    }
}
