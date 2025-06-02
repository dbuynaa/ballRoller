using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private float spawnDistanceAhead = 30f;
    [SerializeField] private float initialSpawnRate = 1f;
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private int numberOfLanes = 3;
    [SerializeField] private Transform roadCenter;

    [Header("Difficulty Settings")]
    [SerializeField] private float minSpawnRate = 0.3f;
    [SerializeField] private float maxSpawnRate = 2f;
    [SerializeField] private float spawnRateIncreasePerPhase = 0.2f;
    [SerializeField] private float obstacleSpeedMultiplier = 1f;
    [SerializeField] private float obstacleSizeMultiplier = 1f;

    [Header("Spawn Randomization")]
    [SerializeField] private float minSpawnDistance = 20f;
    [SerializeField] private float maxSpawnDistance = 40f;
    [SerializeField] private bool allowConsecutiveSameLane = false;
    [SerializeField] private float obstaclePatternChance = 0.3f; // Chance to spawn obstacle patterns

    private float roadWidth = 9f;
    private float nextSpawnTime;
    private PlayerController player;
    private List<GameObject> objectPool;
    private int lastLaneIndex = -1;
    private float currentSpawnRate;
    private int currentPhase = 1;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure there's a PlayerController in the scene.");
        }

        currentSpawnRate = initialSpawnRate;
        SetupRoad();
    }

    private void SetupRoad()
    {
        GameObject road = GameObject.FindGameObjectWithTag("Road");
        if (road != null)
        {
            roadCenter = road.transform;
            
            Collider roadCollider = road.GetComponent<Collider>();
            if (roadCollider != null)
            {
                if (roadCollider is BoxCollider boxCollider)
                {
                    roadWidth = boxCollider.size.x * road.transform.lossyScale.x;
                }
                else if (roadCollider is MeshCollider meshCollider)
                {
                    roadWidth = meshCollider.bounds.size.x;
                }
            }
            else
            {
                MeshRenderer meshRenderer = road.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    roadWidth = meshRenderer.bounds.size.x;
                }
            }
        }
    }
    
    private void Update()
    {
        if (player == null) return;

        // Update spawn rate based on player's speed and phase
        float speedFactor = player.GetCurrentSpeed() / player.GetMaxSpeed();
        currentSpawnRate = Mathf.Lerp(minSpawnRate, maxSpawnRate, speedFactor);
        currentSpawnRate *= (1f + (currentPhase - 1) * spawnRateIncreasePerPhase);

        if (Time.time >= nextSpawnTime)
        {
            if (Random.value < obstaclePatternChance)
            {
                SpawnObstaclePattern();
            }
            else
            {
                SpawnObstacle();
            }
            nextSpawnTime = Time.time + Random.Range(minSpawnRate, currentSpawnRate);
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogWarning("No obstacle prefabs assigned to spawner!");
            return;
        }

        // Get a random lane
        int lane = Random.Range(0, numberOfLanes);
        SpawnObstacleAtLane(lane);
    }

    private void SpawnObstaclePattern()
    {
        // Implement different obstacle patterns based on phase
        switch (currentPhase)
        {
            case 1:
                SpawnSimplePattern();
                break;
            case 2:
                SpawnAlternatingPattern();
                break;
            case 3:
                SpawnWallPattern();
                break;
            default:
                SpawnComplexPattern();
                break;
        }
    }

    private void SpawnSimplePattern()
    {
        // Spawn 2-3 obstacles in a row
        int count = Random.Range(2, 4);
        int startLane = Random.Range(0, numberOfLanes - count + 1);
        
        for (int i = 0; i < count; i++)
        {
            SpawnObstacleAtLane(startLane + i);
        }
    }

    private void SpawnAlternatingPattern()
    {
        // Spawn obstacles in alternating lanes
        for (int i = 0; i < numberOfLanes; i += 2)
        {
            SpawnObstacleAtLane(i);
        }
    }

    private void SpawnWallPattern()
    {
        // Spawn a wall of obstacles
        for (int i = 0; i < numberOfLanes; i++)
        {
            SpawnObstacleAtLane(i);
        }
    }

    private void SpawnComplexPattern()
    {
        // Spawn a more complex pattern with gaps
        int[] pattern = new int[] { 0, 2, 1, 0, 2 };
        foreach (int lane in pattern)
        {
            if (lane < numberOfLanes)
            {
                SpawnObstacleAtLane(lane);
            }
        }
    }

    private void SpawnObstacleAtLane(int lane)
    {
        Vector3 spawnPosition = CalculateSpawnPosition(lane);
        GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
        
        // Scale obstacle based on difficulty
        float scale = 1f + ((currentPhase - 1) * 0.1f) * obstacleSizeMultiplier;
        obstacle.transform.localScale *= scale;
    }

    private Vector3 CalculateSpawnPosition(int lane)
    {
        Vector3 playerForward = player.transform.forward;
        float randomSpawnDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 baseSpawnPos = player.transform.position + playerForward * randomSpawnDistance;
        
        float maxOffset = roadWidth / 2f;
        float lanePosition = lane - ((numberOfLanes - 1) / 2f);
        float laneOffset = lanePosition * laneWidth;
        laneOffset = Mathf.Clamp(laneOffset, -maxOffset, maxOffset);
        
        Vector3 spawnPosition = baseSpawnPos;
        
        if (roadCenter != null)
        {
            Vector3 roadForward = roadCenter.forward;
            Vector3 roadRight = roadCenter.right;
            float distanceAlongRoad = Vector3.Dot(spawnPosition - roadCenter.position, roadForward);
            spawnPosition = roadCenter.position + 
                          roadForward * distanceAlongRoad + 
                          roadRight * laneOffset;
        }
        else
        {
            spawnPosition += player.transform.right * laneOffset;
        }
        
        spawnPosition.y = 0.5f;
        return spawnPosition;
    }

    public void OnDifficultyPhaseChange(int newPhase)
    {
        currentPhase = newPhase;
    }
}
