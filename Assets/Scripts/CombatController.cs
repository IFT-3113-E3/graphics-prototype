using System;
using System.Collections;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    public SwordSlashAnimator slashAnimator;
    public Transform sword;

    private void Start()
    {
        if (slashAnimator == null)
        {
            Debug.LogError("No SwordSlashAnimator assigned to CombatController!");
            return;
        }
        slashAnimator.Configure(new SlashAnimationConfig
        {
            arcAmount = 0.5f,
            length = 1f,
            duration = 0.2f,
            segments = 20,
            color = Color.white
        });
    }

    bool prevSlash = false;
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !prevSlash) {
            ExecuteSlash();
            prevSlash = true;
        }
    }

    IEnumerator SlashAnimSwordDrag() {
        Vector3 startPos = sword.position;
        Vector3 endPos = sword.position + sword.parent.right * 1f;

        Vector3 startDir = sword.forward;
        Vector3 endDir = Quaternion.Euler(0, 90 + 30, 0) * startDir;
        sword.forward = endDir;
        sword.position = endPos;
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
        slashAnimator.PlaySlash(() => {
            sword.position = startPos;
            sword.forward = endDir;
            Debug.Log("Slash animation finished!");
            prevSlash = false;
        });
        yield return new WaitForEndOfFrame();
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
        yield return new WaitForEndOfFrame();
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
        yield return new WaitForEndOfFrame();
        sword.Rotate(0, 10, 0);
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
        yield return new WaitForEndOfFrame();
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
        yield return new WaitForEndOfFrame();
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
        yield return new WaitForEndOfFrame();
        sword.Rotate(0, 10, 0);
        slashAnimator.SetupSlash(startPos, endPos, sword.position, sword.forward);
    }
    
    void ExecuteSlash() {
        
        StartCoroutine(SlashAnimSwordDrag());
        
        //
        // hitboxSpawner.SpawnHitbox(
        //     position: (startPos + endPos) / 2,
        //     rotation: sword.rotation,
        //     size: new Vector3(2f, 1f, 0.5f),
        //     duration: 0.1f,
        //     damage: 15f
        // );
    }
}