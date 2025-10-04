using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public List<AudioClip> startSounds = new List<AudioClip>();
    public float startDelay = 0.5f; // Time to wait before starting game

    private bool isStarting = false;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (startSounds == null || startSounds.Count == 0)
        {
            Debug.LogWarning("[MenuManager] No AudioClip or audioSource set for 'startSounds'");
        }
    }

    // Check for any input to start the game
    void Update()
    {
        if (Input.anyKeyDown)
        {
            if (!isStarting) { StartCoroutine(StartSequence()); }
        }
    }

    // Start level 1 after a feedback sound and short delay
    private IEnumerator StartSequence()
    {
        isStarting = true;

        // Play random sound
        int i = Random.Range(0, startSounds.Count);
        audioSource.PlayOneShot(startSounds[i]);

        // Wait before scene change
        yield return new WaitForSeconds(startDelay);

        isStarting = false;

        // Load level 1
        SceneManager.LoadScene(1);
    }
}
