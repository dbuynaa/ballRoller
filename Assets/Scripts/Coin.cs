using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int baseValue = 1;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobHeight = 0.5f;
    [SerializeField] private GameObject collectEffect; // Optional particle effect for collection
    
    private int currentValue;
    private Vector3 startPosition;
    private float bobTime;

    private void Start()
    {
        currentValue = baseValue;
        startPosition = transform.position;
        bobTime = Random.Range(0f, 2f * Mathf.PI); // Random start phase for bobbing
    }

    private void Update()
    {
        // Rotate the coin
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Bob up and down
        bobTime += bobSpeed * Time.deltaTime;
        float yOffset = Mathf.Sin(bobTime) * bobHeight;
        transform.position = startPosition + new Vector3(0f, yOffset, 0f);
    }

    public void SetValue(int value)
    {
        currentValue = value;
    }

    public int GetBaseValue()
    {
        return baseValue;
    }

    public int GetCurrentValue()
    {
        return currentValue;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add score through GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(currentValue);
            }
            
            // Spawn collection effect if assigned
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy the coin
            Destroy(gameObject);
        }
    }
} 