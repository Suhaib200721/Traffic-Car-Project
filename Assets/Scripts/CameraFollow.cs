using UnityEngine;

/// <summary>
/// Dynamic multi-angle camera follow system for Traffic Escape 3D.
/// Supports switching between Chase, Action, Hood/Cockpit, and Top-Down views,
/// with speed-responsive FOV during Nitro boost.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public enum CameraMode
    {
        Chase = 0,
        Action = 1,
        Hood = 2,
        TopDown = 3
    }

    [Header("Target & Tuning")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 8.5f;

    [Header("Camera Views")]
    [SerializeField] private CameraMode currentMode = CameraMode.Chase;

    // View presets: (offset, lookAhead, fov)
    private readonly Vector3[] viewOffsets = new Vector3[]
    {
        new Vector3(0f, 4.5f, -7.5f),   // Chase (Standard)
        new Vector3(0f, 2.8f, -5.2f),   // Action (Close & Low)
        new Vector3(0f, 1.35f, 0.45f),  // Hood / Cockpit (First-person on road)
        new Vector3(0f, 13.5f, -4.5f)   // Top-Down / Arcade Birds-Eye
    };

    private readonly float[] viewLookAheads = new float[]
    {
        14.0f, // Chase
        16.0f, // Action
        24.0f, // Hood
        7.0f   // Top-Down
    };

    private readonly float[] viewFovs = new float[]
    {
        60.0f, // Chase
        66.0f, // Action
        76.0f, // Hood
        54.0f  // Top-Down
    };

    private Camera cam;
    private CarController carCtrl;

    public CameraMode CurrentMode => currentMode;
    public string CurrentModeName => currentMode switch
    {
        CameraMode.Chase => "CHASE",
        CameraMode.Action => "ACTION",
        CameraMode.Hood => "HOOD",
        CameraMode.TopDown => "TOP-DOWN",
        _ => "CHASE"
    };

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        UpdateCarControllerRef();
    }

    private void Update()
    {
        // Hotkeys C or V to cycle camera views
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V))
        {
            SwitchCameraAngle();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        int modeIdx = Mathf.Clamp((int)currentMode, 0, viewOffsets.Length - 1);
        Vector3 desiredOffset = viewOffsets[modeIdx];
        float lookAhead = viewLookAheads[modeIdx];
        float baseFov = viewFovs[modeIdx];

        // Smooth position follow
        Vector3 desiredPos = target.position + desiredOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // Smooth look-at slightly ahead of the car down the road
        Vector3 lookTarget = target.position + Vector3.forward * lookAhead + Vector3.up * 0.5f;
        transform.LookAt(lookTarget);

        // Dynamic FOV with Nitro boost effect
        if (cam != null)
        {
            float targetFov = baseFov;
            if (carCtrl != null && carCtrl.IsBoosting)
            {
                targetFov += 8.0f; // High-speed sensation during boost
            }
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 6f);
        }
    }

    /// <summary>
    /// Cycles to the next camera perspective.
    /// </summary>
    public void SwitchCameraAngle()
    {
        currentMode = (CameraMode)(((int)currentMode + 1) % 4);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// Explicitly set camera mode.
    /// </summary>
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
    }

    public void SnapToTarget(Transform newTarget)
    {
        target = newTarget;
        UpdateCarControllerRef();

        if (target != null)
        {
            int modeIdx = Mathf.Clamp((int)currentMode, 0, viewOffsets.Length - 1);
            Vector3 desiredOffset = viewOffsets[modeIdx];
            float lookAhead = viewLookAheads[modeIdx];

            transform.position = target.position + desiredOffset;
            Vector3 lookTarget = target.position + Vector3.forward * lookAhead + Vector3.up * 0.5f;
            transform.LookAt(lookTarget);
        }
    }

    private void UpdateCarControllerRef()
    {
        if (target != null)
        {
            carCtrl = target.GetComponent<CarController>() ?? target.GetComponentInParent<CarController>();
        }
    }
}
