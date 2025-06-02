using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float initialSpawnRate = 0.5f;
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private int numberOfLanes = 3;
    [SerializeField] private Transform roadCenter;

    [Header("Difficulty Settings")]
    [SerializeField] private float minSpawnRate = 0.2f;
    [SerializeField] private float maxSpawnRate = 1f;
    [SerializeField] private float spawnRateIncreasePerPhase = 0.1f;
    [SerializeField] private float coinValueMultiplier = 1f;

    [Header("Spawn Randomization")]
    [SerializeField] private float minSpawnDistance = 20f;
    [SerializeField] private float maxSpawnDistance = 40f;
    [SerializeField] private float coinPatternChance = 0.4f;
    [SerializeField] private int minCoinsInPattern = 3;
    [SerializeField] private int maxCoinsInPattern = 6;

    private float roadWidth = 9f;
    private float nextSpawnTime;
    private PlayerController player;
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
            if (Random.value < coinPatternChance)
            {
                SpawnCoinPattern();
            }
            else
            {
                SpawnSingleCoin();
            }
            nextSpawnTime = Time.time + Random.Range(minSpawnRate, currentSpawnRate);
        }
    }

    private void SpawnSingleCoin()
    {
        int lane = Random.Range(0, numberOfLanes);
        SpawnCoinAtLane(lane);
    }

    private void SpawnCoinPattern()
    {
        switch (currentPhase)
        {
            case 1:
                SpawnLinePattern();
                break;
            case 2:
                SpawnZigzagPattern();
                break;
            case 3:
                SpawnDiamondPattern();
                break;
            default:
                SpawnComplexPattern();
                break;
        }
    }

    private void SpawnLinePattern()
    {
        // Spawn coins in a straight line
        int count = Random.Range(minCoinsInPattern, maxCoinsInPattern + 1);
        int lane = Random.Range(0, numberOfLanes);
        float spacing = 2f; // Space between coins

        for (int i = 0; i < count; i++)
        {
            Vector3 position = CalculateSpawnPosition(lane);
            position += player.transform.forward * (i * spacing);
            SpawnCoinAtPosition(position);
        }
    }

    private void SpawnZigzagPattern()
    {
        // Spawn coins in a zigzag pattern
        int count = Random.Range(minCoinsInPattern, maxCoinsInPattern + 1);
        float spacing = 2f;

        for (int i = 0; i < count; i++)
        {
            int lane = i % numberOfLanes;
            Vector3 position = CalculateSpawnPosition(lane);
            position += player.transform.forward * (i * spacing);
            SpawnCoinAtPosition(position);
        }
    }

    private void SpawnDiamondPattern()
    {
        // Spawn coins in a diamond pattern
        int[] lanes = { 1, 0, 1, 2, 1 }; // Center, left, center, right, center
        float spacing = 2f;

        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i] < numberOfLanes)
            {
                Vector3 position = CalculateSpawnPosition(lanes[i]);
                position += player.transform.forward * (i * spacing);
                SpawnCoinAtPosition(position);
            }
        }
    }

    private void SpawnComplexPattern()
    {
        // Spawn coins in a more complex pattern
        int count = Random.Range(minCoinsInPattern, maxCoinsInPattern + 1);
        float spacing = 2f;
        int[] pattern = { 0, 2, 1, 0, 2, 1 };

        for (int i = 0; i < count; i++)
        {
            int lane = pattern[i % pattern.Length];
            if (lane < numberOfLanes)
            {
                Vector3 position = CalculateSpawnPosition(lane);
                position += player.transform.forward * (i * spacing);
                SpawnCoinAtPosition(position);
            }
        }
    }

    private void SpawnCoinAtLane(int lane)
    {
        Vector3 spawnPosition = CalculateSpawnPosition(lane);
        SpawnCoinAtPosition(spawnPosition);
    }

    private void SpawnCoinAtPosition(Vector3 position)
    {
        GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity);
        
        // Set coin value based on difficulty
        Coin coinComponent = coin.GetComponent<Coin>();
        if (coinComponent != null)
        {
            coinComponent.SetValue(Mathf.RoundToInt(coinComponent.GetBaseValue() * (1f + (currentPhase - 1) * coinValueMultiplier)));
        }
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
        
        spawnPosition.y = 1f; // Keep coins slightly above ground
        return spawnPosition;
    }

    public void OnDifficultyPhaseChange(int newPhase)
    {
        currentPhase = newPhase;
    }
} 