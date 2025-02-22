using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class PixelPerfectCamera : MonoBehaviour
{
    public int pixelsPerUnit = 16;
    public int referenceScreenHeight = 640 / 2; // Change this based on your target resolution
    private Camera cam;
    private float gridSize;
    
    void Awake()
    {
        cam = GetComponent<Camera>();
        gridSize = 1f / pixelsPerUnit;
    }

    private void Update()
    {
        // update reference screenheight if changed
        referenceScreenHeight = Screen.height;
        Debug.Log(referenceScreenHeight);
    }

    void LateUpdate()
    {
        // Snap camera position to pixel grid
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
        pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
        pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
        transform.position = pos;

        // Snap orthographic size
        // float targetSize = (referenceScreenHeight / 2f) / pixelsPerUnit;
        // cam.orthographicSize = Mathf.Round(targetSize / gridSize) * gridSize;
    }
}