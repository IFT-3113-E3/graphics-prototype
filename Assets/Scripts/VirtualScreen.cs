using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class VirtualScreen : GraphicRaycaster
{
    [SerializeField] private Camera gameCamera; // Reference to the camera rendering to the RenderTexture
    [SerializeField] private GameCanvasScaler canvasScaler; // The low-resolution RenderTexture
    private Canvas canvas;

    protected override void Awake()
    {
        base.Awake();
        canvas = GetComponent<Canvas>();

        if (gameCamera == null)
        {
            Debug.LogWarning("RenderTextureGraphicRaycaster: No game camera assigned.");
        }

        if (canvasScaler == null)
        {
            Debug.LogWarning("RenderTextureGraphicRaycaster: No render texture assigned.");
        }
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (gameCamera == null || canvasScaler == null || canvas == null)
            return;

        // Convert screen position to the render texture's resolution
        canvasScaler.CalculateRaycastPosition(eventData.position, out Vector2 adjustedPosition);

        // Create a new PointerEventData with the converted position
        eventData.position = adjustedPosition;

        // Perform the usual UI raycast with the adjusted event data
        base.Raycast(eventData, resultAppendList);
    }
}
