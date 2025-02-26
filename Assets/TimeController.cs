using UnityEngine;

public class TimeController : MonoBehaviour
{
    public Transform directionalLight; // Reference to the directional light
    public float transitionDuration = 2f; // Duration of the transition

    private Quaternion[] timeRotations; // Array to store rotations for different times of the day
    private Quaternion targetRotation; // Target rotation for the light
    private float transitionProgress = 1f; // Progress of the current transition

    void Start()
    {
        // Define rotations for different times of the day
        timeRotations = new Quaternion[]
        {
            Quaternion.Euler(0, 36, 0), // Midnight
            Quaternion.Euler(30, 36, 0), // Early morning
            Quaternion.Euler(60, 36, 0), // Morning
            Quaternion.Euler(90, 36, 0), // Noon
            Quaternion.Euler(120, 36, 0), // Afternoon
            Quaternion.Euler(150, 36, 0), // Evening
            Quaternion.Euler(180, 36, 0) // Night
        };

        // Set the initial target rotation to the current rotation
        targetRotation = directionalLight.rotation;
    }

    void Update()
    {
        HandleInput();
        UpdateLightRotation();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTargetRotation(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTargetRotation(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTargetRotation(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetTargetRotation(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetTargetRotation(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetTargetRotation(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetTargetRotation(6);

        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus)) IncrementRotation();
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) DecrementRotation();
    }

    void SetTargetRotation(int index)
    {
        if (index >= 0 && index < timeRotations.Length)
        {
            targetRotation = timeRotations[index];
            transitionProgress = 0f;
        }
    }

    void IncrementRotation()
    {
        int currentIndex = System.Array.IndexOf(timeRotations, targetRotation);
        SetTargetRotation((currentIndex + 1) % timeRotations.Length);
    }

    void DecrementRotation()
    {
        int currentIndex = System.Array.IndexOf(timeRotations, targetRotation);
        SetTargetRotation((currentIndex - 1 + timeRotations.Length) % timeRotations.Length);
    }

    void UpdateLightRotation()
    {
        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            directionalLight.rotation = Quaternion.Slerp(directionalLight.rotation, targetRotation, transitionProgress);
        }
    }
}