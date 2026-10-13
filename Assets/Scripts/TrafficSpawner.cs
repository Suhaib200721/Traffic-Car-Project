using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns oncoming traffic cars, coins, and power-ups ahead of the player.
/// Guarantees at least one lane is always clear so every situation is fair.
/// </summary>
public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject trafficCarPrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject shieldPrefab;

    [Header("Lane Setup")]
    // 3 Lanes: Left (-3.5), Center (0), Right (3.5)
    [SerializeField] private float[] lanePositions = new float[] { -3.5f, 0f, 3.5f };

    [Header("Car Color Materials")]
    [SerializeField] private Material[] carMaterials;

    [Header("Spawning Tuning")]
    [SerializeField] private float spawnDistanceAhead = 55f;
    [SerializeField] private float baseSpawnInterval = 1.9f;
    [SerializeField] private float minSpawnInterval = 1.1f;
    [SerializeField] private float baseTrafficSpeed = 16f;
    [SerializeField] private float maxTrafficSpeed = 24f;
    [SerializeField] private float minCarSpacing = 16f;

    private float timer;
    private float elapsedTime;
    private bool isSpawningActive;
    private float[] lastSpawnZ = new float[] { -999f, -999f, -999f };
    private int totalSpawnedCount = 0;

    public bool IsSpawningActive => isSpawningActive;
    public int TotalSpawnedCars => totalSpawnedCount;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        // 1. Auto-discover player if missing
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
            }
            else
            {
                var cc = FindFirstObjectByType<CarController>() ?? FindObjectOfType<CarController>();
                if (cc != null) playerTransform = cc.transform;
            }
        }

        // 2. Auto-load traffic car prefab if missing
        if (trafficCarPrefab == null)
        {
            trafficCarPrefab = Resources.Load<GameObject>("Prefabs/TrafficCar");
#if UNITY_EDITOR
            if (trafficCarPrefab == null)
            {
                trafficCarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrafficCar.prefab");
            }
#endif
            if (trafficCarPrefab != null)
            {
                Debug.Log("[TrafficSpawner] Auto-resolved trafficCarPrefab fallback from project.");
            }
            else
            {
                Debug.LogError("[TrafficSpawner] CRITICAL: trafficCarPrefab could not be resolved!");
            }
        }

        // 3. Auto-load coin & shield prefabs if missing
        if (coinPrefab == null)
        {
            coinPrefab = Resources.Load<GameObject>("Prefabs/Coin");
#if UNITY_EDITOR
            if (coinPrefab == null)
            {
                coinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin.prefab");
            }
#endif
        }

        if (shieldPrefab == null)
        {
            shieldPrefab = Resources.Load<GameObject>("Prefabs/ShieldPowerUp");
#if UNITY_EDITOR
            if (shieldPrefab == null)
            {
                shieldPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShieldPowerUp.prefab");
            }
#endif
        }

        // 4. Auto-load traffic materials if missing
#if UNITY_EDITOR
        if (carMaterials == null || carMaterials.Length == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("Mat_Traffic", new[] { "Assets/Materials" });
            if (guids.Length > 0)
            {
                carMaterials = new Material[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    carMaterials[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
        }
#endif
    }

    private void Update()
    {
        // Auto-synchronize with GameManager state
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing && !isSpawningActive)
            {
                Debug.Log("[TrafficSpawner] Auto-activating spawner (GameManager state: Playing).");
                StartSpawning();
            }
            else if (GameManager.Instance.CurrentState != GameManager.GameState.Playing && isSpawningActive)
            {
                StopSpawning();
            }
        }
        else if (!isSpawningActive && playerTransform != null)
        {
            // Standalone scene testing without GameManager
            StartSpawning();
        }

        if (!isSpawningActive || playerTransform == null) return;

        elapsedTime += Time.deltaTime;
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnWave();
            timer = CalculateCurrentSpawnInterval();
        }
    }

    public void StartSpawning()
    {
        EnsureReferences();
        isSpawningActive = true;
        elapsedTime = 0f;
        timer = 0.5f; // Fast initial spawn so player sees oncoming cars right away

        for (int i = 0; i < lastSpawnZ.Length; i++)
        {
            lastSpawnZ[i] = -999f;
        }

        Debug.Log($"[TrafficSpawner] Spawner STARTED. Prefab: {(trafficCarPrefab != null ? trafficCarPrefab.name : "NULL!")}, Player: {(playerTransform != null ? playerTransform.name : "NULL!")}, SpawnDist: {spawnDistanceAhead}m.");
    }

    public void StopSpawning()
    {
        isSpawningActive = false;
        Debug.Log("[TrafficSpawner] Spawner STOPPED.");
    }

    private float CalculateCurrentSpawnInterval()
    {
        // Difficulty scaling over 90 seconds
        float t = Mathf.Clamp01(elapsedTime / 90f);
        return Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
    }

    private float CalculateCurrentTrafficSpeed()
    {
        float t = Mathf.Clamp01(elapsedTime / 90f);
        return Mathf.Lerp(baseTrafficSpeed, maxTrafficSpeed, t);
    }

    private void SpawnWave()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[TrafficSpawner] Spawn attempt skipped: playerTransform is NULL!");
            return;
        }

        if (trafficCarPrefab == null)
        {
            Debug.LogError("[TrafficSpawner] Spawn attempt skipped: trafficCarPrefab is NULL!");
            return;
        }

        float currentSpeed = CalculateCurrentTrafficSpeed();
        float spawnZ = playerTransform.position.z + spawnDistanceAhead;

        // Shuffle lanes indices [0, 1, 2]
        List<int> lanes = new List<int> { 0, 1, 2 };
        for (int i = 0; i < lanes.Count; i++)
        {
            int temp = lanes[i];
            int randomIndex = Random.Range(i, lanes.Count);
            lanes[i] = lanes[randomIndex];
            lanes[randomIndex] = temp;
        }

        // Determine number of cars in this wave:
        // Always leave at least one lane completely clear! (Max 2 out of 3 lanes)
        int carsToSpawn = Random.value < 0.45f ? 2 : 1;
        if (elapsedTime > 45f)
        {
            carsToSpawn = Random.value < 0.65f ? 2 : 1;
        }

        // Spawn traffic cars in chosen lanes
        for (int i = 0; i < carsToSpawn; i++)
        {
            int laneIdx = lanes[i];
            float laneX = lanePositions[laneIdx];
            Vector3 spawnPos = new Vector3(laneX, 0.35f, spawnZ);

            // Overlap check 1: Distance from last car in this lane
            float distFromLast = Mathf.Abs(spawnZ - lastSpawnZ[laneIdx]);
            if (distFromLast < minCarSpacing)
            {
                Debug.Log($"[TrafficSpawner] Skipped Lane {laneIdx} (Z={spawnZ:F1}): Lane busy with recent vehicle ({distFromLast:F1}m < {minCarSpacing}m).");
                continue;
            }

            // Overlap check 2: Check ONLY for other vehicles (elevated above road surface to never hit road/ground)
            Vector3 checkCenter = spawnPos + Vector3.up * 0.65f;
            Vector3 checkExtents = new Vector3(1.1f, 0.5f, 3.5f);
            Collider[] hits = Physics.OverlapBox(checkCenter, checkExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            bool vehicleBlocked = false;
            foreach (var h in hits)
            {
                if (h != null && (h.CompareTag("Traffic") || h.GetComponentInParent<TrafficCar>() != null || h.CompareTag("Player")))
                {
                    vehicleBlocked = true;
                    Debug.Log($"[TrafficSpawner] Skipped Lane {laneIdx} (Z={spawnZ:F1}): Occupied by vehicle '{h.name}'.");
                    break;
                }
            }
            if (vehicleBlocked)
            {
                continue;
            }

            // Instantiate oncoming traffic car facing the player (180 deg rotation)
            GameObject carObj = Instantiate(trafficCarPrefab, spawnPos, Quaternion.Euler(0f, 180f, 0f));
            TrafficCar trafficCar = carObj.GetComponent<TrafficCar>();

            Material mat = null;
            if (carMaterials != null && carMaterials.Length > 0)
            {
                mat = carMaterials[Random.Range(0, carMaterials.Length)];
            }

            if (trafficCar != null)
            {
                trafficCar.Initialize(playerTransform, currentSpeed + Random.Range(-1.0f, 1.0f), mat);
            }

            lastSpawnZ[laneIdx] = spawnZ;
            totalSpawnedCount++;

            Debug.Log($"[TrafficSpawner] >> SPAWNED oncoming car #{totalSpawnedCount} in Lane {laneIdx} (X={laneX:F1}) at {spawnPos}, Direction: {carObj.transform.forward}, Speed: {currentSpeed:F1} m/s.");
        }

        // The remaining lane is guaranteed CLEAR.
        // Spawn collectible coins or shield power-ups in the clear lane.
        int clearLaneIdx = lanes[lanes.Count - 1];
        float clearLaneX = lanePositions[clearLaneIdx];

        // 60% chance to spawn coins in the clear lane
        if (coinPrefab != null && Random.value < 0.60f)
        {
            int coinCount = Random.Range(1, 4);
            for (int c = 0; c < coinCount; c++)
            {
                Vector3 coinPos = new Vector3(clearLaneX, 0.7f, spawnZ + 4f + (c * 3.5f));
                Collider[] coinHits = Physics.OverlapBox(coinPos, new Vector3(0.5f, 0.4f, 1.0f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
                bool blocked = false;
                foreach (var ch in coinHits)
                {
                    if (ch != null && (ch.CompareTag("Traffic") || ch.GetComponentInParent<TrafficCar>() != null || ch.CompareTag("Player")))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    GameObject coinObj = Instantiate(coinPrefab, coinPos, Quaternion.identity);
                    Coin coin = coinObj.GetComponent<Coin>();
                    if (coin != null) coin.Initialize(playerTransform);
                }
            }
        }
        // 12% chance to spawn a shield power-up in the clear lane
        else if (shieldPrefab != null && Random.value < 0.12f)
        {
            Vector3 shieldPos = new Vector3(clearLaneX, 0.75f, spawnZ + 5f);
            Collider[] shieldHits = Physics.OverlapBox(shieldPos, new Vector3(0.6f, 0.5f, 1.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            bool blocked = false;
            foreach (var sh in shieldHits)
            {
                if (sh != null && (sh.CompareTag("Traffic") || sh.GetComponentInParent<TrafficCar>() != null || sh.CompareTag("Player")))
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
            {
                GameObject shieldObj = Instantiate(shieldPrefab, shieldPos, Quaternion.identity);
                ShieldPowerUp shield = shieldObj.GetComponent<ShieldPowerUp>();
                if (shield != null) shield.Initialize(playerTransform);
            }
        }
    }

    public void ClearAllSpawnedObjects()
    {
        for (int i = 0; i < lastSpawnZ.Length; i++)
        {
            lastSpawnZ[i] = -999f;
        }

        // Find and destroy all active traffic, coins, and shields
        TrafficCar[] cars = FindObjectsByType<TrafficCar>(FindObjectsSortMode.None);
        foreach (var car in cars)
        {
            if (car != null) Destroy(car.gameObject);
        }

        Coin[] coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
        foreach (var coin in coins)
        {
            if (coin != null) Destroy(coin.gameObject);
        }

        ShieldPowerUp[] shields = FindObjectsByType<ShieldPowerUp>(FindObjectsSortMode.None);
        foreach (var s in shields)
        {
            if (s != null) Destroy(s.gameObject);
        }
    }
}
