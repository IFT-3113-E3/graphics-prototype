using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelPerfectCamera : MonoBehaviour
{
    public int ppu = 32;
    public bool pixelSnapping = true;
    
    private int _textureWidth;
    private int _textureHeight;
    private float _pixelStep; // Smallest movement step
    private Camera _cam;
    
    public Transform virtualCamera;
    
    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _textureWidth = _cam.pixelWidth;
        _textureHeight = _cam.pixelHeight;
        
        Debug.Log($"Texture size: {_textureWidth}x{_textureHeight}");
    }

    private void Start()
    {
        _pixelStep = 1.0f / ppu;
        _cam.transform.rotation = Quaternion.Euler(Mathf.Atan(1.0f/Mathf.Sqrt(2.0f)) * Mathf.Rad2Deg, 0, 0);
        _cam.orthographicSize = _textureHeight / (2.0f * ppu);
    }

    private void LateUpdate()
    {
        // Keep following the target while applying pixel snapping
        transform.position = pixelSnapping ? RoundToPixel(virtualCamera.position) : virtualCamera.position;
        transform.rotation = virtualCamera.rotation;
    }

    public Vector3 RoundToPixel(Vector3 position)
    {
        float unitsPerPixel = _pixelStep; // Adjust based on your needs
        if (unitsPerPixel == 0.0f)
            return position;

        // Transform position into camera's local space
        Quaternion inverseRotation = Quaternion.Inverse(_cam.transform.rotation);
        Vector3 localPosition = inverseRotation * position;

        // Snap to pixel grid in local space
        localPosition.x = Mathf.Round(localPosition.x / unitsPerPixel) * unitsPerPixel;
        localPosition.y = Mathf.Round(localPosition.y / unitsPerPixel) * unitsPerPixel;
        localPosition.z = Mathf.Round(localPosition.z / unitsPerPixel) * unitsPerPixel;

        // Convert back to world space
        return _cam.transform.rotation * localPosition;
    }

}