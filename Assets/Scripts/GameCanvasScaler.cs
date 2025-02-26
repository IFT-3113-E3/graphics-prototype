using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GameCanvasScaler : MonoBehaviour
{
    public enum DisplayMode
    {
        Fit,
        Stretch,
        Original
    }

    [SerializeField] private RawImage rawImage;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Fit;

    private RectTransform _rectTransform;

    private int _lastScreenWidth, _lastScreenHeight;

    private bool _resized;

    private void Awake()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        _rectTransform = rawImage.GetComponent<RectTransform>();
        rawImage.texture = renderTexture;
        _resized = true;
    }

    private void Start()
    {
        UpdateScaling();
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _resized = true;
        }
    }

    private void LateUpdate()
    {
        if (_resized)
        {
            _resized = false;
            UpdateScaling();
        }
    }
    
    public void SetDisplayMode(DisplayMode mode)
    {
        displayMode = mode;
        _resized = true;
    }

    private void UpdateScaling()
    {
        if (!renderTexture || !_rectTransform) return;

        float screenAspect = (float)Screen.width / Screen.height;
        float textureAspect = (float)renderTexture.width / renderTexture.height;

        switch (displayMode)
        {
            case DisplayMode.Stretch:
                _rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
                rawImage.rectTransform.localScale = Vector3.one;
                break;

            case DisplayMode.Original:
                _rectTransform.sizeDelta = new Vector2(renderTexture.width, renderTexture.height);
                rawImage.rectTransform.localScale = Vector3.one;
                break;

            case DisplayMode.Fit:
                if (textureAspect > screenAspect)
                {
                    _rectTransform.sizeDelta =
                        new Vector2(Screen.width, Screen.width / textureAspect);
                }
                else // Taller
                {
                    _rectTransform.sizeDelta =
                        new Vector2(Screen.height * textureAspect, Screen.height);
                }

                // round up the width and height to prevent subpixel artifacts
                _rectTransform.sizeDelta = new Vector2(
                    Mathf.Ceil(_rectTransform.sizeDelta.x),
                    Mathf.Ceil(_rectTransform.sizeDelta.y)
                );
                
                rawImage.rectTransform.localScale = Vector3.one;
                break;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Awake();
    }
    
    private void OnRectTransformDimensionsChange()
    {
        _resized = true;
    }
#endif
}