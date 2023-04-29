using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Components + GameObjects
    private CharacterController _characterController;
    private Transform _mainCamera;
    private CinemachineFreeLook _cameraController;
    public Animator playerAnimator;

    // Input values
    private bool _isGameActive = true;
    private Vector2 _currentInput = new Vector2();
    private bool _isSprinting = false;
    private bool _isCrouching = false;
    private bool _jumpedThisFrame = false;
    private bool _rotateCamera = false;

    [SerializeField]
    private float movementSpeed = 0.25f;
    [SerializeField]
    private float sprintModifier = 0.25f;
    [SerializeField]
    private float coyoteTime = 0.25f;

    // Crouching
    
    [SerializeField]
    private float crouchModifier = 0.25f;
    [SerializeField]
    private float crouchColliderHeight = 0.5f;
    [SerializeField]
    private float crouchColliderYOffset = 0.5f;
    private Vector3 _crouchCenter;
    private float _crouchHeight;

    // Jumping
    [SerializeField]
    private int numberOfJumps = 1;
    [SerializeField]
    private int jumpHeight = 1;
    private int _remainingJumps;
    private float _timeSinceGrounded = 0f;

    // Falling and gravity
    [SerializeField]
    private Vector3 verticalVelocity;
    [SerializeField]
    private float gravityValue = -9.81f;
    [SerializeField]
    private float fallingModifier = 1f;

    // Turning
    [SerializeField]
    private float turnDegreesPerSecond = 90;
    private float _turnTime = 0.1f;
    private float _turnSmoothVelocity;
    
    private void Start() {
        _characterController = GetComponent<CharacterController>();
        _crouchHeight = _characterController.height;
        _crouchCenter = _characterController.center;
        _mainCamera = Camera.main.transform;
        _cameraController = (CinemachineFreeLook)(((CinemachineBrain)(_mainCamera.GetComponent<CinemachineBrain>())).ActiveVirtualCamera);
        // Cursor.visible = false;
    }

    public void Jump(InputAction.CallbackContext context) {
        if (context.performed) {
            _jumpedThisFrame = true;
        }
    }

    public void Sprint(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Performed) {
            _isSprinting = true;
        }
        else if (context.phase == InputActionPhase.Canceled) {
            _isSprinting = false;
        }
    }
    
    public void Crouch(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Performed) {
            _isCrouching = true;
            _characterController.height = crouchColliderHeight;
            _characterController.center = new Vector3(0, crouchColliderYOffset, 0);
        }
        else if (context.phase == InputActionPhase.Canceled) {
            _isCrouching = false;
            _characterController.center = _crouchCenter;
            _characterController.height = _crouchHeight;
        }
    }

    public void Move(InputAction.CallbackContext context) {
        _currentInput = context.ReadValue<Vector2>();
    }
    
    public void TurnCamera(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Performed) {
            _rotateCamera = true;
        }
        else if (context.phase == InputActionPhase.Canceled) {
            _rotateCamera = false;
        }
    }

    void Update() {
        if(Mathf.Approximately(Time.timeScale, 0f)){
            return;
        }
        // Set inital variables
        bool isGrounded = _characterController.isGrounded;

        // Reset jumping values if grounded
        if (isGrounded) {
            _timeSinceGrounded = 0;
            _remainingJumps = numberOfJumps;
        }
        else {
            _timeSinceGrounded += Time.deltaTime;
        }

        if(_currentInput.y == 0){
            if (playerAnimator != null){
                playerAnimator.SetFloat("movementDirection", Mathf.Abs(_currentInput.x));
            }
        }
        else {
            if (playerAnimator != null)
                playerAnimator.SetFloat("movementDirection", _currentInput.y);
        }

        // Gravity
        if (isGrounded && verticalVelocity.y <= 0f) {
            // Constant drag to make sure the player is always grounded
            verticalVelocity.y = -0.5f;
        }
        else {
            // Apply gravity to the player
            if (verticalVelocity.y >= 0) {
                verticalVelocity.y += gravityValue * Time.deltaTime;
            }
            else {
                // Possible additional modifier to increase the falling speed (may feel better for player)
                verticalVelocity.y += gravityValue * Time.deltaTime * fallingModifier;
            }
        }

        // Conditions for jumping: on the ground, in the air for less time than coyote time or jumped before already
        if (_jumpedThisFrame && (isGrounded ||(_timeSinceGrounded < coyoteTime && _remainingJumps > 0) || (_remainingJumps > 0 && _remainingJumps < numberOfJumps))) {
            if (isGrounded || verticalVelocity.y >= 0) {
                verticalVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                verticalVelocity.y = Mathf.Min(verticalVelocity.y, Mathf.Sqrt(jumpHeight * -3.0f * gravityValue));
            }
            else {
                verticalVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue) + Mathf.Abs(verticalVelocity.y);
            }
            _remainingJumps--;
        }
        _jumpedThisFrame = false;
        
        if (playerAnimator != null)
            playerAnimator.SetBool("isJumping", !isGrounded);
        if (playerAnimator != null)
            playerAnimator.SetFloat("jumpValue", verticalVelocity.y);


        if (_currentInput.magnitude > 0f || !_rotateCamera) {
            // Simulate 8D movement => reduce directional shift if both forward/backward + left/right are pressed
            float offset = (_currentInput.x * 90) / (1 + Mathf.Abs(_currentInput.y));
            if (_currentInput.y < 0)
            {
                offset *= -1;
            }

            float targetRotation = 0;
            if (_rotateCamera) {
                targetRotation = transform.rotation.eulerAngles.y + offset;
            }
            else {
                targetRotation = _mainCamera.eulerAngles.y + offset;
            }
            
            float previousY = transform.rotation.eulerAngles.y;

            float yAngle = Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, targetRotation,
                ref _turnSmoothVelocity, _turnTime, turnDegreesPerSecond);
            transform.rotation = Quaternion.Euler(0f, yAngle, 0f);

            float yChange = yAngle - previousY;
            if (playerAnimator != null)
                playerAnimator.SetFloat("currentRotation", yChange);

            // Rewrite: move also sidewards! Take left/right (currentInput.x) into consideration!
            Vector3 movementVector = transform.forward * _currentInput.y;
            if (movementVector.magnitude == 0)
            {
                movementVector = transform.forward * Mathf.Abs(_currentInput.x);
            }

            Vector3 baseSpeed = movementVector.normalized * movementSpeed;
            Vector3 sprintSpeed = ((1 - Convert.ToInt32(_isCrouching)) * Convert.ToInt32(_isSprinting) *
                                   sprintModifier * baseSpeed);
            Vector3 crouchSpeed = (Convert.ToInt32(_isCrouching) * crouchModifier * baseSpeed);
            _characterController.Move((baseSpeed + crouchSpeed + sprintSpeed + verticalVelocity) * Time.deltaTime);
        }
    }
}
