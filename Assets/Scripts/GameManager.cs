// GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI; // Add this for Button component
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Game Settings")]
    [SerializeField] private float gameOverY = -10f;

    [Header("Difficulty Settings")]
    [SerializeField] private float phaseDuration = 60f; // Duration of each difficulty phase in seconds
    [SerializeField] private float scoreMultiplierPerPhase = 1.1f; // Score multiplier increase per phase
    [SerializeField] private TextMeshProUGUI phaseText; // UI element to show current phase

    [Header("Score Settings")]
    [SerializeField] private int score = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private float distanceScore = 0f;
    [SerializeField] private float coinScore = 0f;
    [SerializeField] private bool showDebugInfo = false;

    private PlayerController player;
    private bool isGameOver;
    private int currentPhase = 1;
    private float phaseTimer = 0f;
    private float currentScoreMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        isGameOver = false;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void Start()
    {
        // Initial setup
        FindPlayer();
        FindUIReferences();
        
        // Ensure gameOverPanel is initially hidden
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Load high score
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from scene loaded event
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded: " + scene.name);
        // Wait for one frame to ensure all objects are properly initialized
        StartCoroutine(InitializeAfterSceneLoad());
    }

    private IEnumerator InitializeAfterSceneLoad()
    {
        // Wait for one frame
        yield return null;
        
        // Re-initialize references
        FindPlayer();
        FindUIReferences();
        
        // Reset game state
        isGameOver = false;
        Time.timeScale = 1f;
        
        // Ensure gameOverPanel is hidden
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Debug.Log("Scene initialization complete");
    }

    private void FindUIReferences()
    {
        Debug.Log("Finding UI references...");
        
        // Find the Canvas first
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("Main Canvas not found in scene!");
            return;
        }

        Debug.Log("Found Canvas: " + mainCanvas.name);
        
        // Find UI elements within the Canvas
        Transform canvasTransform = mainCanvas.transform;
        
        // Find ScoreText
        Transform scoreTextTransform = canvasTransform.Find("ScoreText");
        if (scoreTextTransform != null)
        {
            scoreText = scoreTextTransform.GetComponent<TextMeshProUGUI>();
            Debug.Log("Found ScoreText");
        }
        else
        {
            Debug.LogWarning("ScoreText not found in Canvas!");
        }

        // Find CoinText
        Transform coinTextTransform = canvasTransform.Find("CoinText");
        if (coinTextTransform != null)
        {
            coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
            Debug.Log("Found CoinText");
        }
        else
        {
            Debug.LogWarning("CoinText not found in Canvas!");
        }



        // Find SpeedText
        Transform speedTextTransform = canvasTransform.Find("SpeedText");
        if (speedTextTransform != null)
        {
            speedText = speedTextTransform.GetComponent<TextMeshProUGUI>();
            Debug.Log("Found SpeedText");
        }
        else
        {
            Debug.LogWarning("SpeedText not found in Canvas!");
        }

        // Find GameOverPanel
        Transform gameOverPanelTransform = canvasTransform.Find("GameOverPanel");
        if (gameOverPanelTransform != null)
        {
            gameOverPanel = gameOverPanelTransform.gameObject;
            Debug.Log("Found GameOverPanel");
        }
        else
        {
            Debug.LogWarning("GameOverPanel not found in Canvas!");
        }

        // Find FinalScoreText within GameOverPanel
        if (gameOverPanel != null)
        {
            Transform finalScoreTextTransform = gameOverPanel.transform.Find("Final Score");
            if (finalScoreTextTransform != null)
            {
                finalScoreText = finalScoreTextTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log("Found FinalScoreText");
            }
            else
            {
                Debug.LogWarning("FinalScoreText not found in GameOverPanel!");
            }
        }
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("Player not found in scene!");
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        // Ensure we have a player reference
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // Update phase timer
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= phaseDuration)
        {
            phaseTimer = 0f;
            currentPhase++;
            currentScoreMultiplier *= scoreMultiplierPerPhase;
            OnDifficultyPhaseChange(currentPhase);
        }

        // Update UI
        if (scoreText != null)
        {
            scoreText.text = $"Score: {GetScore()}";
        }
        if (coinText != null)
        {
            coinText.text = $"Coin: {GetCoinScore()}";
        }
        if (speedText != null)
        {
            speedText.text = $"Speed: {player.GetCurrentSpeed():F1}";
        }
        if (phaseText != null)
        {
            phaseText.text = $"Phase: {currentPhase}";
        }

        // Check for game over
        if (player.transform.position.y < gameOverY)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        if (isGameOver) return; // Prevent multiple calls
        
        isGameOver = true;
        Time.timeScale = 0f;
        
        // Get final score
        int finalScore = GetScore();
        
        // Save final score
        PlayerPrefs.SetInt("LastScore", finalScore);
        
        // Update high score
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        // Save all PlayerPrefs
        PlayerPrefs.Save();

        // Update UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score: {finalScore}\nHigh Score: {highScore}";
            }
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        isGameOver = false;
        Time.timeScale = 1f;
        
        // Reset scores
        score = 0;
        distanceScore = 0f;
        coinScore = 0f;
        
        if(gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Load the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            SceneManager.LoadScene(0); // Or go back to menu
    }

    public void OnDifficultyPhaseChange(int newPhase)
    {
        currentPhase = newPhase;
        
        // Notify spawners of phase change
        Spawner[] spawners = FindObjectsOfType<Spawner>();
        foreach (var spawner in spawners)
        {
            spawner.OnDifficultyPhaseChange(newPhase);
        }
        
        CoinSpawner[] coinSpawners = FindObjectsOfType<CoinSpawner>();
        foreach (var spawner in coinSpawners)
        {
            spawner.OnDifficultyPhaseChange(newPhase);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Difficulty phase changed to: {newPhase}");
        }
    }

    public void AddScore(int value)
    {
        coinScore += value;
        UpdateTotalScore();
        if (showDebugInfo)
        {
            Debug.Log($"Coin collected! Added {value} points. Total coin score: {coinScore}");
        }
    }

    public void AddDistanceScore(float value)
    {
        distanceScore += value;
        UpdateTotalScore();
    }

    private void UpdateTotalScore()
    {
        score = Mathf.RoundToInt(coinScore + distanceScore);
        
        // Update high score if needed
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            
            if (showDebugInfo)
            {
                Debug.Log($"New high score: {highScore}!");
            }
        }
    }

    public int GetScore()
    {
        return score;
    }

    public int GetHighScore()
    {
        return highScore;
    }

    public float GetDistanceScore()
    {
        return distanceScore;
    }

    public float GetCoinScore()
    {
        return coinScore;
    }
}
