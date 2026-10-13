using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game controller managing game states, score, high score, lives, and flow.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    [Header("Core References")]
    [SerializeField] private CarController playerCar;
    [SerializeField] private TrafficSpawner trafficSpawner;
    [SerializeField] private RoadManager roadManager;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private UIManager uiManager;

    [Header("Gameplay Config")]
    [SerializeField] private int maxLives = 1;

    public enum MissionType
    {
        TravelDistance,
        CollectCoins,
        SurviveTime
    }

    [System.Serializable]
    public struct MissionData
    {
        public string description;
        public MissionType type;
        public float targetValue;
        public int bonusScore;

        public MissionData(string desc, MissionType t, float target, int bonus)
        {
            description = desc;
            type = t;
            targetValue = target;
            bonusScore = bonus;
        }
    }

    // State
    private GameState currentState = GameState.Menu;
    private int currentLives;
    private int currentScore;
    private int highScore;
    private int coinCount;
    private float distanceTraveled;
    private float survivalTime;
    private float startZ = 0f;
    private bool isNewHighScore;

    // Mission System State
    private MissionData[] missions;
    private int currentMissionIndex = 0;
    private float missionBannerTimer = 0f;
    private string completedMissionNotice = "";
    private int missionBonusScoreAccumulator = 0;

    private static bool autoStartOnLoad = false;

    private const string HighScoreKey = "TRAFFIC_ESCAPE_HIGH_SCORE";

    public GameState CurrentState => currentState;
    public int CurrentLives => currentLives;
    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public int CoinCount => coinCount;
    public float DistanceTraveled => distanceTraveled;
    public float SurvivalTime => survivalTime;
    public bool IsNewHighScore => isNewHighScore;
    public float DisplaySpeedKmh => playerCar != null ? playerCar.CurrentSpeed * 3.2f : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize Mission Set
        missions = new MissionData[]
        {
            new MissionData("Travel 250 Meters", MissionType.TravelDistance, 250f, 150),
            new MissionData("Collect 5 Coins", MissionType.CollectCoins, 5f, 200),
            new MissionData("Survive for 30 Seconds", MissionType.SurviveTime, 30f, 250),
            new MissionData("Travel 600 Meters", MissionType.TravelDistance, 600f, 350),
            new MissionData("Collect 12 Coins", MissionType.CollectCoins, 12f, 400),
            new MissionData("Survive for 60 Seconds", MissionType.SurviveTime, 60f, 500)
        };

        // Load saved high score from PlayerPrefs
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (playerCar == null)
            playerCar = FindFirstObjectByType<CarController>() ?? FindObjectOfType<CarController>();
        if (trafficSpawner == null)
            trafficSpawner = FindFirstObjectByType<TrafficSpawner>() ?? FindObjectOfType<TrafficSpawner>();
        if (roadManager == null)
            roadManager = FindFirstObjectByType<RoadManager>() ?? FindObjectOfType<RoadManager>();
        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>() ?? FindObjectOfType<CameraFollow>();
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>() ?? FindObjectOfType<UIManager>();
    }

    private void Start()
    {
        EnsureReferences();
        if (autoStartOnLoad)
        {
            autoStartOnLoad = false;
            StartGame();
        }
        else
        {
            ReturnToMainMenu();
        }
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateGameplayMetrics();

            // ESC or P toggles pause
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                PauseGame();
            }
        }
        else if (currentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                ResumeGame();
            }
        }
    }

    private void UpdateGameplayMetrics()
    {
        if (playerCar == null) return;

        // Survival time in seconds
        survivalTime += Time.deltaTime;

        // Distance in meters
        distanceTraveled = Mathf.Max(0f, playerCar.transform.position.z - startZ);

        // Score based on distance travelled + survival time + collected coins bonus + completed mission bonuses
        int calculatedScore = Mathf.FloorToInt(distanceTraveled * 0.5f + survivalTime * 2f) + (coinCount * 25) + missionBonusScoreAccumulator;
        currentScore = calculatedScore;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        // Check active missions
        CheckMissionProgress();

        // Banner timer
        if (missionBannerTimer > 0f)
        {
            missionBannerTimer -= Time.deltaTime;
            if (missionBannerTimer <= 0f)
            {
                completedMissionNotice = "";
            }
        }
    }

    private void CheckMissionProgress()
    {
        if (missions == null || currentMissionIndex >= missions.Length) return;

        MissionData active = missions[currentMissionIndex];
        if (active.type == MissionType.TravelDistance && distanceTraveled >= active.targetValue)
        {
            CompleteMission();
        }
        else if (active.type == MissionType.SurviveTime && survivalTime >= active.targetValue)
        {
            CompleteMission();
        }
    }

    private void CompleteMission()
    {
        if (missions == null || currentMissionIndex >= missions.Length) return;

        MissionData completed = missions[currentMissionIndex];
        missionBonusScoreAccumulator += completed.bonusScore;
        currentScore += completed.bonusScore;

        completedMissionNotice = $"MISSION COMPLETED!\n+{completed.bonusScore} BONUS PTS";
        missionBannerTimer = 3.5f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMissionCompleteSound();

        currentMissionIndex++;
    }

    public string GetActiveMissionDescription()
    {
        if (missions == null || currentMissionIndex >= missions.Length)
            return "ALL MISSIONS COMPLETED!";

        MissionData m = missions[currentMissionIndex];
        return m.type switch
        {
            MissionType.TravelDistance => $"MISSION: {m.description} ({Mathf.FloorToInt(distanceTraveled)}/{Mathf.FloorToInt(m.targetValue)}m)",
            MissionType.CollectCoins => $"MISSION: {m.description} ({coinCount}/{Mathf.FloorToInt(m.targetValue)})",
            MissionType.SurviveTime => $"MISSION: {m.description} ({Mathf.FloorToInt(survivalTime)}/{Mathf.FloorToInt(m.targetValue)}s)",
            _ => $"MISSION: {m.description}"
        };
    }

    public string GetMissionBannerNotice()
    {
        return missionBannerTimer > 0f ? completedMissionNotice : "";
    }

    public void StartGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        isNewHighScore = false;
        currentLives = maxLives;
        currentScore = 0;
        coinCount = 0;
        distanceTraveled = 0f;
        survivalTime = 0f;
        startZ = 0f;
        currentMissionIndex = 0;
        missionBannerTimer = 0f;
        completedMissionNotice = "";
        missionBonusScoreAccumulator = 0;

        // Reset world components
        Vector3 carStartPos = new Vector3(0f, 0.35f, 0f);
        if (playerCar != null)
        {
            playerCar.ResetCar(carStartPos);
            playerCar.EnableControls(true);
        }

        if (cameraFollow != null && playerCar != null)
        {
            cameraFollow.SnapToTarget(playerCar.transform);
        }

        if (roadManager != null && playerCar != null)
        {
            roadManager.ResetRoad(playerCar.transform);
        }

        if (trafficSpawner != null)
        {
            trafficSpawner.ClearAllSpawnedObjects();
            trafficSpawner.StartSpawning();
        }

        if (uiManager != null)
        {
            uiManager.ShowHUD();
        }
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        if (playerCar != null) playerCar.EnableControls(false);

        if (uiManager != null)
            uiManager.ShowPausePanel();
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        if (playerCar != null) playerCar.EnableControls(true);

        if (uiManager != null)
            uiManager.ShowHUD();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        autoStartOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        autoStartOnLoad = false;
        currentState = GameState.Menu;
        Time.timeScale = 1f;

        if (playerCar != null)
        {
            playerCar.ResetCar(new Vector3(0f, 0.35f, 0f));
            playerCar.EnableControls(false);
        }

        if (trafficSpawner != null)
        {
            trafficSpawner.StopSpawning();
            trafficSpawner.ClearAllSpawnedObjects();
        }

        if (roadManager != null && playerCar != null)
        {
            roadManager.ResetRoad(playerCar.transform);
        }

        if (cameraFollow != null && playerCar != null)
        {
            cameraFollow.SnapToTarget(playerCar.transform);
        }

        if (uiManager != null)
        {
            uiManager.ShowMainMenu();
        }
    }

    public void LoseLife()
    {
        if (currentState != GameState.Playing) return;

        currentLives--;
        if (currentLives <= 0)
        {
            currentLives = 0;
            GameOver();
        }
    }

    public void AddCoin(int points)
    {
        if (currentState != GameState.Playing) return;

        coinCount++;
        currentScore += points;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (missions != null && currentMissionIndex < missions.Length)
        {
            MissionData active = missions[currentMissionIndex];
            if (active.type == MissionType.CollectCoins && coinCount >= active.targetValue)
            {
                CompleteMission();
            }
        }
    }

    public void GameOver()
    {
        currentState = GameState.GameOver;
        Time.timeScale = 0f;

        if (playerCar != null)
            playerCar.EnableControls(false);

        if (trafficSpawner != null)
            trafficSpawner.StopSpawning();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameOverSound();

        // Finalize high score save
        if (currentScore > highScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
        }
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save();

        if (uiManager != null)
            uiManager.ShowGameOverPanel();
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
