using UnityEngine;

public class RotatingTransform : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 0, 0);
    
    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);
    }
}