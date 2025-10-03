using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Setup")]
    public List<GameObject> tilePrefabs = new List<GameObject>();
    public Transform tileSpawnParent;
    public Vector3 tileSpawnOffset = new Vector3(-3, 0, 0);
    public float tileSpacing = 1.5f;

    [Header("Victory Settings")]
    public float victoryDelay = 2.0f; // Time to show victory before reset
    public bool autoResetOnVictory = true;
    
    [Header("Audio")]
    public AudioClip victorySound;
    public AudioClip placeTileSound;
    public AudioClip resetSound;

    private List<GameObject> currentTiles = new List<GameObject>();
    private List<TileInitialState> initialTileStates = new List<TileInitialState>();
    private AudioSource audioSource;
    private bool isVictorySequencePlaying = false;


    // Track states of tiles for checking placement (and subsequently checking winstate)
    // Matches indices of 'currentTiles'
    private List<bool> currentTilesGrabbed = new List<bool>();


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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        // Setup win condition event listener
        if (WinCondition.Instance != null)
        {
            WinCondition.Instance.onWinConditionMet.AddListener(OnVictory);
        }
        else
        {
            Debug.LogError("WinCondition not found in scene! Please add a WinCondition component.");
        }

        SetupLevel();
        //SetupTestLevel();
    }

    void Update()
    {
        // Continously monitor tiles to see if they are released
        MonitorTiles();
    }

    void OnDestroy()
    {
        // Cleanup event listener
        if (WinCondition.Instance != null)
        {
            WinCondition.Instance.onWinConditionMet.RemoveListener(OnVictory);
        }
    }

    // Update the currentTilesGrabbed list from the tiles we have in the current game - tracking their grab/release state
    private void MonitorTiles()
    {
        if (isVictorySequencePlaying) return; // Don't check during victory sequence 

        for (int i = 0; i < currentTiles.Count; i++)
        {
            // For each tracked tile in the game scene
            GameObject tile = currentTiles[i];
            TileVertices tileVertices = tile.GetComponent<TileVertices>();

            // Check its previous grab state
            bool currentlyGrabbed = tileVertices.isGrabbed;
            bool previouslyGrabbed = currentTilesGrabbed[i];
            // If the tile was previously grabbed and currently is not (i.e. it was just released)
            if (previouslyGrabbed && !currentlyGrabbed)
            {
                // Trigger tile place and win-state check
                OnTilePlaced();
            }

            // Update tile state for this tile index 'i'
            currentTilesGrabbed[i] = currentlyGrabbed;
        }
    }

    public void SetupLevel()
    {
        // Clear previous tiles
        foreach (GameObject tile in currentTiles)
        {
            if (tile != null)
            {
                Destroy(tile);
            }
        }
        currentTiles.Clear();
        initialTileStates.Clear();

        // Spawn tiles
        for (int i = 0; i < tilePrefabs.Count; i++)
        {
            if (tilePrefabs[i] == null)
            {
                Debug.LogWarning($"Tile prefab at index {i} is null!");
                continue;
            }

            Vector3 spawnPos = tileSpawnOffset + Vector3.right * (i * tileSpacing);
            GameObject tile = Instantiate(tilePrefabs[i], spawnPos, Quaternion.identity, tileSpawnParent);
            currentTiles.Add(tile);

            // Store initial state for reset
            TileInitialState state = new TileInitialState
            {
                prefabIndex = i,
                position = spawnPos,
                rotation = Quaternion.identity,
                scale = tile.transform.localScale
            };
            initialTileStates.Add(state);
        }

        currentTilesGrabbed = new List<bool>(currentTiles.Count);
        for (int i = 0; i < currentTiles.Count; i++) currentTilesGrabbed.Add(false);

        Debug.Log($"✓ Level setup complete with {currentTiles.Count} tiles");
    }

    /// <summary>
    /// Called by TileVertices when a tile is placed
    /// </summary>
    public void OnTilePlaced()
    {
        if (isVictorySequencePlaying)
        {
            return; // Don't check during victory sequence
        }

        // Play sound
        if (placeTileSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(placeTileSound);
        }

        // Trigger win condition check
        if (WinCondition.Instance != null)
        {
            WinCondition.Instance.CheckWinConditionDelayed();
        }
    }

    private void OnVictory()
    {
        if (isVictorySequencePlaying)
        {
            return; // Already playing victory sequence
        }

        Debug.Log("🎉 VICTORY! Puzzle solved!");

        // Play victory sound
        if (victorySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // Start victory sequence
        if (autoResetOnVictory)
        {
            StartCoroutine(VictorySequence());
        }
    }

    private IEnumerator VictorySequence()
    {
        isVictorySequencePlaying = true;

        // Disable tile interaction
        foreach (GameObject tile in currentTiles)
        {
            if (tile != null)
            {
                TileVertices tileVert = tile.GetComponent<TileVertices>();
                if (tileVert != null)
                {
                    tileVert.enabled = false;
                }

                // Optional: Add visual feedback (bounce, glow, etc.)
                StartCoroutine(VictoryTileBounce(tile));
            }
        }

        // Wait before reset
        yield return new WaitForSeconds(victoryDelay);

        // Reset the level
        ResetLevel();

        isVictorySequencePlaying = false;
    }

    private IEnumerator VictoryTileBounce(GameObject tile)
    {
        if (tile == null) yield break;

        Vector3 originalPos = tile.transform.position;
        float bounceHeight = 0.3f;
        float bounceSpeed = 3f;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * bounceSpeed;
            float bounce = Mathf.Sin(elapsed * Mathf.PI) * bounceHeight;
            tile.transform.position = originalPos + Vector3.up * bounce;
            yield return null;
        }

        tile.transform.position = originalPos;
    }

    public void ResetLevel()
    {
        Debug.Log("Resetting level...");

        // Play reset sound
        if (resetSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resetSound);
        }

        // Destroy current tiles
        foreach (GameObject tile in currentTiles)
        {
            if (tile != null)
            {
                Destroy(tile);
            }
        }
        currentTiles.Clear();

        // Recreate tiles from initial states
        foreach (TileInitialState state in initialTileStates)
        {
            if (state.prefabIndex < tilePrefabs.Count && tilePrefabs[state.prefabIndex] != null)
            {
                GameObject tile = Instantiate(
                    tilePrefabs[state.prefabIndex], 
                    state.position, 
                    state.rotation, 
                    tileSpawnParent
                );
                tile.transform.localScale = state.scale;
                currentTiles.Add(tile);
            }
        }
        
        currentTilesGrabbed = new List<bool>(currentTiles.Count);
        for (int i = 0; i < currentTiles.Count; i++) currentTilesGrabbed.Add(false);

        Debug.Log("✓ Level reset complete");
    }

    /// <summary>
    /// Setup a simple test level for debugging
    /// </summary>
    public void SetupTestLevel()
    {
        if (WinCondition.Instance == null)
        {
            Debug.LogError("Cannot setup test level - WinCondition not found!");
            return;
        }

        // Create a simple square outline
        List<Vector2> squareOutline = new List<Vector2>
        {
            new Vector2(-1, -1),
            new Vector2(1, -1),
            new Vector2(1, 1),
            new Vector2(-1, 1)
        };
        
        WinCondition.Instance.SetTargetOutline(squareOutline);
        
        // Add a color target in the center (should be yellow = red + green)
        WinCondition.Instance.AddColorTarget(Vector2.zero, Color.yellow);
        
        Debug.Log("✓ Test level configured: 2x2 square with yellow center");
    }

    /// <summary>
    /// Load a specific level configuration
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        Debug.Log($"Loading level {levelNumber}...");
        
        // You would implement level-specific configurations here
        // For now, just reset
        ResetLevel();
    }
}

[System.Serializable]
public class TileInitialState
{
    public int prefabIndex;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}