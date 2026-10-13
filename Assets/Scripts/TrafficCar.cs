using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls an oncoming traffic car driving towards the player.
/// </summary>
public class TrafficCar : MonoBehaviour
{
    [SerializeField] private float speed = 16f;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Renderer[] headlightRenderers;

    private Transform playerTransform;
    private bool isFlashing;

    public float CurrentSpeed => speed;

    private void Awake()
    {
        EnsureComponents();
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var cc = FindFirstObjectByType<CarController>() ?? FindObjectOfType<CarController>();
                if (cc != null) playerTransform = cc.transform;
            }
        }

        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (bodyRenderer == null)
        {
            var rends = GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (r != null && (r.gameObject.name.Contains("Chassis") || r.gameObject.name.Contains("Body")))
                {
                    bodyRenderer = r;
                    break;
                }
            }
        }

        if (headlightRenderers == null || headlightRenderers.Length == 0)
        {
            var rends = GetComponentsInChildren<Renderer>();
            var list = new List<Renderer>();
            foreach (var r in rends)
            {
                if (r != null && r.gameObject.name.Contains("Headlight"))
                    list.Add(r);
            }
            headlightRenderers = list.ToArray();
        }
    }

    public void Initialize(Transform player, float trafficSpeed, Material colorMat)
    {
        playerTransform = player;
        speed = Mathf.Max(10f, trafficSpeed);

        EnsureComponents();

        if (bodyRenderer != null && colorMat != null)
        {
            bodyRenderer.material = colorMat;
        }
    }

    private void Update()
    {
        // Smoothly drive oncoming traffic down the road in negative Z direction
        float currentSpeed = speed;

        // Front obstacle check: If another traffic car is directly ahead in this lane, match its speed to prevent overlapping
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.back);
        RaycastHit[] hits = Physics.RaycastAll(ray, 7.5f);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                if (hit.collider.CompareTag("Traffic") || hit.collider.GetComponentInParent<TrafficCar>() != null)
                {
                    TrafficCar carAhead = hit.collider.GetComponent<TrafficCar>() ?? hit.collider.GetComponentInParent<TrafficCar>();
                    if (carAhead != null && carAhead != this)
                    {
                        currentSpeed = Mathf.Min(currentSpeed, carAhead.speed);
                        break;
                    }
                }
            }
        }

        transform.Translate(Vector3.back * currentSpeed * Time.deltaTime, Space.World);

        // Despawn once well behind player
        if (playerTransform != null)
        {
            if (transform.position.z < playerTransform.position.z - 25f)
            {
                Destroy(gameObject);
            }
        }
        else if (transform.position.z < -60f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// React to player's horn by flashing oncoming headlights.
    /// </summary>
    public void OnPlayerHonked()
    {
        if (!gameObject.activeInHierarchy || isFlashing) return;
        StartCoroutine(FlashHeadlightsRoutine());
    }

    private IEnumerator FlashHeadlightsRoutine()
    {
        isFlashing = true;
        EnsureComponents();

        for (int i = 0; i < 2; i++)
        {
            SetHeadlightsVisible(false);
            yield return new WaitForSeconds(0.08f);
            SetHeadlightsVisible(true);
            yield return new WaitForSeconds(0.08f);
        }
        isFlashing = false;
    }

    private void SetHeadlightsVisible(bool visible)
    {
        if (headlightRenderers != null)
        {
            foreach (var r in headlightRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
