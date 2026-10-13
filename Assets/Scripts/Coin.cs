using UnityEngine;

/// <summary>
/// Collectible gold coin on the road.
/// </summary>
public class Coin : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 160f;
    [SerializeField] private int scoreValue = 10;

    private Transform playerTransform;
    private Vector3 basePosition;
    private bool isCollected = false;

    public void Initialize(Transform player)
    {
        playerTransform = player;
        basePosition = transform.position;
        isCollected = false;
    }

    private void Update()
    {
        // Rotate smoothly
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Despawn if passed
        if (playerTransform != null && transform.position.z < playerTransform.position.z - 15f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ensure each coin can only be collected once
        if (isCollected) return;

        if (other.CompareTag("Player") || other.GetComponent<CarController>() != null || other.GetComponentInParent<CarController>() != null)
        {
            isCollected = true;

            // Immediately disable collider to prevent any duplicate trigger events
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCoinSound();

            if (GameManager.Instance != null)
                GameManager.Instance.AddCoin(scoreValue);

            Destroy(gameObject);
        }
    }
}
