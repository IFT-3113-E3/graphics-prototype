
using UnityEngine;

/*
    This file has a commented version with details about how each line works. 
    The commented version contains code that is easier and simpler to read. This file is minified.
*/


/// <summary>
/// Main script for third-person movement of the character in the game.
/// Make sure that the object that will receive this script (the player) 
/// has the Player tag and the Character Controller component.
/// </summary>
public class ThirdPersonController : MonoBehaviour
{
    private static readonly int Crouch = Animator.StringToHash("crouch");
    private static readonly int Run = Animator.StringToHash("run");
    private static readonly int Sprint = Animator.StringToHash("sprint");
    private static readonly int Air = Animator.StringToHash("air");

    [Tooltip("Speed ​​at which the character moves. It is not affected by gravity or jumping.")]
    public float velocity = 5f;
    [Tooltip("This value is added to the speed value while the character is sprinting.")]
    public float sprintAdittion = 3.5f;
    [Tooltip("The higher the value, the higher the character will jump.")]
    public float jumpForce = 18f;
    [Tooltip("Stay in the air. The higher the value, the longer the character floats before falling.")]
    public float jumpTime = 0.85f;
    [Space]
    [Tooltip("Force that pulls the player down. Changing this value causes all movement, jumping and falling to be changed as well.")]
    public float gravity = 9.8f;

    private float _jumpElapsedTime = 0;

    // Player states
    private bool _isJumping = false;
    private bool _isSprinting = false;
    private bool _isCrouching = false;

    // Inputs
    private float _inputHorizontal;
    private float _inputVertical;
    private bool _inputJump;
    private bool _inputCrouch;
    private bool _inputSprint;

    private Animator _animator;
    private CharacterController _cc;
    private Camera _camera;


    void Start()
    {
        _camera = Camera.main;
        _cc = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        // Message informing the user that they forgot to add an animator
        if (_animator == null)
            Debug.LogWarning("Hey buddy, you don't have the Animator component in your player. Without it, the animations won't work.");
        
        // cut down animator frames for a sprite-sheet animation look
        // remove interpolation between frames
        // if (_animator)
        // {
        //     _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        //     _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        // }
    }

    public int FPS = 12;
    private float _time;
    
    
    // Update is only being used here to identify keys and trigger animations
    void Update()
    {
        _time += Time.deltaTime;
        var updateTime = 1f / FPS;
        _animator.speed = 0;

        if (_time > updateTime)
        {
            _time -= updateTime;
            _animator.speed = updateTime / Time.deltaTime;
        }
        
        // Input checkers
        _inputHorizontal = Input.GetAxis("Horizontal");
        _inputVertical = Input.GetAxis("Vertical");
        _inputJump = Mathf.Approximately(Input.GetAxis("Jump"), 1f);
        _inputSprint = Mathf.Approximately(Input.GetAxis("Fire3"), 1f);
        // Unfortunately GetAxis does not work with GetKeyDown, so inputs must be taken individually
        _inputCrouch = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.JoystickButton1);

        // Check if you pressed the crouch input key and change the player's state
        if ( _inputCrouch )
            _isCrouching = !_isCrouching;

        // Run and Crouch animation
        // If dont have animator component, this block wont run
        if ( _cc.isGrounded && _animator )
        {
            // Crouch
            // Note: The crouch animation does not shrink the character's collider
            _animator.SetBool(Crouch, _isCrouching);
            
            // Run
            float minimumSpeed = 0.9f;
            _animator.SetBool(Run, _cc.velocity.magnitude > minimumSpeed );

            // Sprint
            _isSprinting = _cc.velocity.magnitude > minimumSpeed && _inputSprint;
            _animator.SetBool(Sprint, _isSprinting );

        }

        // Jump animation
        if( _animator )
            _animator.SetBool(Air, _cc.isGrounded == false );

        // Handle can jump or not
        if ( _inputJump && _cc.isGrounded )
        {
            _isJumping = true;
            // Disable crounching when jumping
            //isCrouching = false; 
        }

        HeadHittingDetect();

    }


    // With the inputs and animations defined, FixedUpdate is responsible for applying movements and actions to the player
    private void FixedUpdate()
    {
        
        // Sprinting velocity boost or crounching desacelerate
        float velocityAddition = 0;
        if ( _isSprinting )
            velocityAddition = sprintAdittion;
        if (_isCrouching)
            velocityAddition =  - (velocity * 0.50f); // -50% velocity

        // Direction movement
        float directionX = _inputHorizontal * (velocity + velocityAddition) * Time.deltaTime;
        float directionZ = _inputVertical * (velocity + velocityAddition) * Time.deltaTime;
        float directionY = 0;

        // Jump handler
        if ( _isJumping )
        {

            // Apply inertia and smoothness when climbing the jump
            // It is not necessary when descending, as gravity itself will gradually pulls
            directionY = Mathf.SmoothStep(jumpForce, jumpForce * 0.30f, _jumpElapsedTime / jumpTime) * Time.deltaTime;

            // Jump timer
            _jumpElapsedTime += Time.deltaTime;
            if (_jumpElapsedTime >= jumpTime)
            {
                _isJumping = false;
                _jumpElapsedTime = 0;
            }
        }

        // Add gravity to Y axis
        directionY -= gravity * Time.deltaTime;

        
        // --- Character rotation --- 

        if (!_camera) return;
        Vector3 forward = _camera.transform.forward;
        Vector3 right = _camera.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Relate the front with the Z direction (depth) and right with X (lateral movement)
        forward *= directionZ;
        right *= directionX;

        if (directionX != 0 || directionZ != 0)
        {
            float angle = Mathf.Atan2(forward.x + right.x, forward.z + right.z) * Mathf.Rad2Deg;

            var rotation =
                // snap rotation to 8 directions
                Quaternion.Euler(0, Mathf.Round(angle / 45) * 45, 0);
            
            transform.rotation = rotation;
        }
        
        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 horizontalDirection = forward + right;

        Vector3 moviment = verticalDirection + horizontalDirection;
        _cc.Move( moviment );
    }


    //This function makes the character end his jump if he hits his head on something
    void HeadHittingDetect()
    {
        float headHitDistance = 1.1f;
        Vector3 ccCenter = transform.TransformPoint(_cc.center);
        float hitCalc = _cc.height / 2f * headHitDistance;

        // Uncomment this line to see the Ray drawed in your characters head
        // Debug.DrawRay(ccCenter, Vector3.up * headHeight, Color.red);

        if (Physics.Raycast(ccCenter, Vector3.up, hitCalc))
        {
            _jumpElapsedTime = 0;
            _isJumping = false;
        }
    }

}
