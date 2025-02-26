using UnityEngine;
using UnityEngine.UI;

public class DebugMenuUI : MonoBehaviour
{
    public Button toggleEffectButton;
    
    private void Start()
    {
        toggleEffectButton.onClick.AddListener(ToggleEffect);
    }
    
    private void ToggleEffect()
    {
        Debug.Log("Toggle effect button clicked!");
    }
    
}