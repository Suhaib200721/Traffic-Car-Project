using UnityEngine;

/// <summary>
/// Collectible shield power-up protecting player from one crash.
/// </summary>
public class ShieldPowerUp : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 120f;

    private Transform playerTransform;

    public void Initialize(Transform player)
    {
        playerTransform = player;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (playerTransform != null && transform.position.z < playerTransform.position.z - 15f)
        {
            Destroy(gameObject);
        }
    }

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        CarController player = other.GetComponent<CarController>() ?? other.GetComponentInParent<CarController>();
        if (player != null)
        {
            isCollected = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            player.GiveShield();
            Destroy(gameObject);
        }
    }
}
