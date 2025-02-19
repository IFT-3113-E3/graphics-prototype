using System;
using UnityEngine;
using Unity.Logging;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Log.Warning("Multiple GameManager in scene!");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Setup and initial configuration
        // ...
        Log.Info("Game Manager has been instantiated.");
    }

    private void Start()
    {
        Log.Info("Starting Game Manager");
    }
}