using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the player car movement, steering, boost, and collision handling.
/// </summary>
public class CarController : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float baseSpeed = 22f;          // Base forward speed (~65 km/h)
    [SerializeField] private float minSpeed = 14f;           // Brake minimum speed
    [SerializeField] private float maxSpeed = 32f;           // Normal acceleration max speed
    [SerializeField] private float steerSpeed = 10f;         // Left/Right lane movement speed
    [SerializeField] private float roadBoundary = 3.8f;      // Clamped X boundary for 3 lanes

    [Header("Boost / Nitro Settings")]
    [SerializeField] private float boostMultiplier = 1.45f;  // Speed multiplier during boost
    [SerializeField] private float boostDuration = 2.5f;     // Boost active duration
    [SerializeField] private float boostCooldown = 5.0f;     // Boost recharge cooldown

    [Header("Shield Settings")]
    [SerializeField] private float shieldDuration = 15.0f;   // Shield expires after 15s if not consumed

    [Header("Visual Feedback")]
    [SerializeField] private float tiltAngle = 12f;          // Banking tilt when steering
    [SerializeField] private GameObject shieldVisual;        // Child shield sphere/bubble
    [SerializeField] private Renderer[] carRenderers;        // Renderers to flash during invulnerability

    // Runtime state
    private float currentForwardSpeed;
    private float targetForwardSpeed;
    private bool isBoosting;
    private float boostTimer;
    private float boostCooldownTimer;
    private bool isInvulnerable;
    private bool hasShield;
    private float shieldTimer;
    private bool isControlEnabled;

    public float CurrentSpeed => currentForwardSpeed;
    public float BoostCooldownNormalized => Mathf.Clamp01(1f - (boostCooldownTimer / boostCooldown));
    public bool IsBoosting => isBoosting;
    public bool HasShield => hasShield;
    public float ShieldTimeRemaining => Mathf.Max(0f, shieldTimer);

    private void Start()
    {
        currentForwardSpeed = baseSpeed;
        targetForwardSpeed = baseSpeed;
        if (carRenderers == null || carRenderers.Length == 0)
            carRenderers = GetComponentsInChildren<Renderer>();

        if (shieldVisual == null)
        {
            Transform sb = transform.Find("ShieldBubble");
            if (sb != null) shieldVisual = sb.gameObject;
        }

        if (shieldVisual != null)
            shieldVisual.SetActive(false);
    }

    public void EnableControls(bool enable)
    {
        isControlEnabled = enable;
        if (enable)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.StartEngine();
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopEngine();

            // Cancel active boost when controls are disabled (e.g. pause or game over)
            CancelBoost();
        }
    }

    private void Update()
    {
        if (!isControlEnabled) return;

        HandleInput();
        HandleBoost();
        HandleShield();
        UpdateAudioPitch();
    }

    private void HandleShield()
    {
        if (hasShield)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
            {
                hasShield = false;
                shieldTimer = 0f;
                if (shieldVisual != null)
                    shieldVisual.SetActive(false);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isControlEnabled) return;

        MoveCar();
    }

    private float horizontalInput;

    private void HandleInput()
    {
        // Smooth horizontal steering input (A/D or Left/Right arrows)
        horizontalInput = Input.GetAxis("Horizontal");

        // Forward / Brake throttle (W/S or Up/Down arrows)
        float vInput = Input.GetAxisRaw("Vertical");
        if (vInput > 0.1f)
            targetForwardSpeed = maxSpeed;
        else if (vInput < -0.1f)
            targetForwardSpeed = minSpeed;
        else
            targetForwardSpeed = baseSpeed;

        // Boost input (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateBoost();
        }

        // Horn input (H or E)
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.E))
        {
            HonkHorn();
        }

        // Camera switch input (C or V)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V))
        {
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollow>();
                if (camFollow != null) camFollow.SwitchCameraAngle();
            }
        }
    }

    private float lastHornTime;

    public void HonkHorn()
    {
        if (!isControlEnabled) return;
        if (Time.time - lastHornTime < 0.3f) return;
        lastHornTime = Time.time;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHornSound();

        // Alert oncoming traffic cars ahead of player
        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.forward * 15f, 25f);
        foreach (var hit in hits)
        {
            if (hit != null && (hit.CompareTag("Traffic") || hit.GetComponent<TrafficCar>() != null || hit.GetComponentInParent<TrafficCar>() != null))
            {
                TrafficCar tc = hit.GetComponent<TrafficCar>() ?? hit.GetComponentInParent<TrafficCar>();
                if (tc != null) tc.OnPlayerHonked();
            }
        }
    }

    private void HandleBoost()
    {
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                isBoosting = false;
                boostCooldownTimer = boostCooldown;
            }
        }
        else if (boostCooldownTimer > 0f)
        {
            boostCooldownTimer -= Time.deltaTime;
        }
    }

    public bool ActivateBoost()
    {
        if (!isControlEnabled || isBoosting || boostCooldownTimer > 0f)
            return false;

        isBoosting = true;
        boostTimer = boostDuration;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBoostSound();
        return true;
    }

    public void CancelBoost()
    {
        if (isBoosting)
        {
            isBoosting = false;
            boostTimer = 0f;
            boostCooldownTimer = boostCooldown;
        }
    }

    private void MoveCar()
    {
        // Smoothly adjust forward speed
        float targetSpeedWithBoost = targetForwardSpeed * (isBoosting ? boostMultiplier : 1f);
        currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeedWithBoost, Time.fixedDeltaTime * 4f);

        // Horizontal steering smoothly clamped to playable road
        Vector3 pos = transform.position;
        pos.x += horizontalInput * steerSpeed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, -roadBoundary, roadBoundary);

        // Move forward along Z
        pos.z += currentForwardSpeed * Time.fixedDeltaTime;
        transform.position = pos;

        // Smooth visual tilt when steering
        float targetTilt = -horizontalInput * tiltAngle;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetTilt);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 10f);
    }

    private void UpdateAudioPitch()
    {
        if (AudioManager.Instance != null)
        {
            float speedRatio = currentForwardSpeed / baseSpeed;
            AudioManager.Instance.SetEnginePitch(speedRatio);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Traffic") || other.GetComponent<TrafficCar>() != null || other.GetComponentInParent<TrafficCar>() != null)
        {
            TrafficCar tc = other.GetComponent<TrafficCar>() ?? other.GetComponentInParent<TrafficCar>();
            OnHitTraffic(tc != null ? tc.gameObject : other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Traffic") || collision.gameObject.GetComponent<TrafficCar>() != null)
        {
            TrafficCar tc = collision.gameObject.GetComponent<TrafficCar>() ?? collision.gameObject.GetComponentInParent<TrafficCar>();
            OnHitTraffic(tc != null ? tc.gameObject : collision.gameObject);
        }
    }

    private void OnHitTraffic(GameObject trafficObj)
    {
        if (isInvulnerable) return;

        // Clean up or disable the hit traffic car
        Destroy(trafficObj);

        if (hasShield)
        {
            // Shield absorbs the blow
            hasShield = false;
            shieldTimer = 0f;
            if (shieldVisual != null) shieldVisual.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShieldSound();
            StartCoroutine(InvulnerabilityRoutine(1.2f));
            return;
        }

        // Damage player
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCrashSound();

        if (GameManager.Instance != null)
            GameManager.Instance.LoseLife();

        StartCoroutine(InvulnerabilityRoutine(1.8f));
    }

    public void GiveShield(float duration = 15.0f)
    {
        hasShield = true;
        shieldTimer = duration > 0f ? duration : shieldDuration;
        if (shieldVisual != null)
            shieldVisual.SetActive(true);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShieldSound();
    }

    private IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;
        float elapsed = 0f;
        float blinkRate = 0.12f;

        while (elapsed < duration)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkRate);
            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate * 2f;
        }

        SetRenderersVisible(true);
        isInvulnerable = false;
    }

    private void SetRenderersVisible(bool visible)
    {
        if (carRenderers == null) return;
        foreach (var r in carRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    public void ResetCar(Vector3 startPosition)
    {
        StopAllCoroutines();
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        currentForwardSpeed = baseSpeed;
        targetForwardSpeed = baseSpeed;
        isBoosting = false;
        boostTimer = 0f;
        boostCooldownTimer = 0f;
        isInvulnerable = false;
        hasShield = false;
        shieldTimer = 0f;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        SetRenderersVisible(true);
    }
}
