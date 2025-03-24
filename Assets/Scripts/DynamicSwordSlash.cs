using System;
using System.Collections;
using UnityEngine;

public class DynamicSwordSlash : MonoBehaviour
{
    [Header("Slash Detection Settings")]
    public float motionThreshold = 1.0f;
    public float cooldown = 0.2f;
    public int followUpFrames = 1;
    public SlashAnimationConfig slashConfig;
    
    private SwordSlashAnimator _slashAnimator;
    private Vector3 _previousBladeDirection;
    private Vector3 _motionDirection;
    private float _currentVelocity;

    private float _slashTimer;
    private bool _isSlashing;

    
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private float _cooldownTimer;
    
    private Coroutine _slashCoroutine;
    
    void Start()
    {

        _slashAnimator = new GameObject().AddComponent<SwordSlashAnimator>();
        _slashAnimator.name = "Dynamic Sword Slash Animator";
        
        _slashAnimator.transform.SetParent(null, false);
        
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;
    }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        Vector3 movementDelta = currentPosition - _previousPosition;
        float distanceMoved = movementDelta.magnitude;

        if (distanceMoved > Mathf.Epsilon && _cooldownTimer <= 0f)
        {
            Vector3 bladeDirection = transform.right;

            float bladeAlignedMovement = Vector3.Dot(movementDelta.normalized, bladeDirection);

            if (Mathf.Abs(bladeAlignedMovement) <= 1f && distanceMoved >= motionThreshold)
            {
                TriggerSlashEffect(_previousPosition, currentPosition, _previousRotation * Vector3.forward, currentRotation * Vector3.forward, bladeAlignedMovement);
                _cooldownTimer = cooldown;
            }
        }

        _previousPosition = currentPosition;
        _previousRotation = currentRotation;
    }

    void TriggerSlashEffect(Vector3 start, Vector3 end, Vector3 startDirection, Vector3 endDirection, float alignment)
    {
        if (_slashCoroutine != null)
        {
            StopCoroutine(_slashCoroutine);
        }
        _slashCoroutine = StartCoroutine(SlashAnimSwordDrag(start, end, startDirection, endDirection, alignment));
    }
    
    IEnumerator SlashAnimSwordDrag(Vector3 start, Vector3 end, Vector3 startDirection, Vector3 endDirection, float alignment) {
        _slashAnimator.Configure(slashConfig);
        _slashAnimator.SetupSlash(start, end, startDirection, endDirection, alignment < 0f);
        _slashAnimator.PlaySlash();
        for (int i = 0; i < followUpFrames; i++)
        {
            yield return 0;
            Vector3 currentPosition = transform.position;
            Vector3 currentDirection = transform.forward;
            _slashAnimator.SetupSlash(start, currentPosition, startDirection, currentDirection, alignment < 0f);
        }
    }
}
