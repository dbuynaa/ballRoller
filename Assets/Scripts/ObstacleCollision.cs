// ObstacleCollision.cs
using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [SerializeField] private float pushForce = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Push the player away from the obstacle
                Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                
                // Trigger game over
                GameManager.Instance.GameOver();
            }
        }
    }
}
