#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Monitors user activity in the Editor/Game view during Play Mode.
/// Automatically exits Play Mode after a specified period of inactivity.
/// Attach this script to any GameObject in the initial scene (e.g., a "Manager" object).
/// It will persist across scene loads using DontDestroyOnLoad.
/// </summary>
public class PlayModeIdleExit : MonoBehaviour
{
    [Tooltip("Time in minutes of inactivity after which Play Mode will be exited.")]
    [SerializeField] private float idleMinutes = 5f;

    [SerializeField] private float idleTimeSeconds;          // Converted minutes to seconds
    [SerializeField] private float lastActivityTime;          // Real-time of the last detected activity
    private Vector3 lastMousePosition;       // For tracking mouse movement

    public bool isActive = true;
    
    private void Awake()
    {
        // Ensure this object persists when loading new scenes
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Initialize idle threshold and reset activity timer
        idleTimeSeconds = idleMinutes * 60f;
        ResetActivityTimer();

        // Subscribe to Editor update event
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // Only monitor activity while the Editor is in Play Mode
        if (!EditorApplication.isPlaying)
            return;

        // Detect user activity (keyboard, mouse clicks, mouse movement)
        isActive = false;
        

        // Keyboard key pressed
        if (Input.anyKeyDown)
            isActive = true;

        // Mouse button clicked
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            isActive = true;

        // Mouse moved (compare with previous position)
        Vector3 currentMousePos = Input.mousePosition;
        if (currentMousePos != lastMousePosition)
            isActive = true;

        // Update last mouse position for next frame
        lastMousePosition = currentMousePos;

        // If any activity was detected, reset the idle timer
        if (isActive)
            ResetActivityTimer();

        // Check if idle timeout has been reached
        if (Time.realtimeSinceStartup - lastActivityTime >= idleTimeSeconds)
        {
            Debug.Log($"No activity for {idleMinutes} minute(s). Exiting Play Mode.");
            EditorApplication.isPlaying = false;
            
        }
    }

    /// <summary>
    /// Resets the last activity time to the current real-time.
    /// Also updates the stored mouse position to avoid false positives after reset.
    /// </summary>
    private void ResetActivityTimer()
    {
        lastActivityTime = Time.realtimeSinceStartup;
        lastMousePosition = Input.mousePosition;
    }

    // Optional: You can also provide a public method to change the idle threshold at runtime
    public void SetIdleMinutes(float minutes)
    {
        idleMinutes = minutes;
        idleTimeSeconds = idleMinutes * 60f;
    }
}
#endif