using System;
using NaughtyAttributes;
using UnityEngine;
using static L3.Scripts.AudioUtils;

namespace L3.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [SerializeField] 
        [BoxGroup("Movement Settings")]
        private float walkSpeed = 5.0f;

        [SerializeField] 
        [BoxGroup("Movement Settings")]
        private float sprintSpeed = 10.0f;

        [SerializeField]
        [BoxGroup("Movement Settings")]
        private float accelerationTime = 0.3f;

        [SerializeField] 
        [BoxGroup("Movement Settings")]
        private float decelerationTime = 0.2f;

        [SerializeField] 
        [BoxGroup("Movement Settings")]
        private float sprintTransitionTime = 0.5f;

        [SerializeField]
        [BoxGroup("Jump Settings")]
        private float jumpHeight = 1.5f;

        [SerializeField] 
        [BoxGroup("Jump Settings")]
        private float jumpCooldown = 0.1f;

        [SerializeField] 
        [BoxGroup("Camera Settings")]
        private Camera playerCamera;

        [SerializeField] 
        [BoxGroup("Camera Settings")]
        private float mouseSensitivity = 2.0f;

        [SerializeField] 
        [BoxGroup("Camera Settings")]
        private float verticalLookLimit = 80.0f;

        [SerializeField] 
        [BoxGroup("Ground collision settings")]
        private LayerMask groundLayers;

        [SerializeField]
        [BoxGroup("Physics settings")]
        private float gravity = 10f;

        [SerializeField]
        [BoxGroup("Footsteps settings")]
        private float stepLength = 2;

        [SerializeField]
        [BoxGroup("External components")]
        private CharacterController characterController;

        public event Action<AudioSurfaceType, float> OnFootstepDetected;
    
        private float currentSpeed;
        private float targetSpeed;
        private float verticalRotation;
        private float verticalVelocity;
        private float speedSmoothVelocity;
        private float currentSpeedPercent;
        private float jumpCooldownTimer;

        private Vector3 moveDirection;
        private bool isGrounded;
        private bool wasGroundedLastFrame;
        private bool didJumpThisFrame;

        private float distanceTravelled;
        private Vector3 lastPosition;

        private void Start()
        {
            // Lock cursor to center of screen.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Initialize speed.
            currentSpeed = 0f;
            targetSpeed = 0f;

            lastPosition = transform.position;
        }

        private void Update()
        {
            UpdateGroundedState();
            HandleJump();
            HandleMouseLook();
            HandleMovement();
            HandleFootsteps();

            // Update jump cooldown.
            if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;

            // Remember grounded state for next frame.
            wasGroundedLastFrame = isGrounded;
        }

        private void HandleMouseLook()
        {
            // Horizontal rotation (rotate player as well).
            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            transform.Rotate(Vector3.up, mouseX);

            // Vertical rotation (camera only).
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void HandleJump()
        {
            didJumpThisFrame = false;

            if (!Input.GetKeyDown(KeyCode.Space))         return;
            if (!isGrounded || !(jumpCooldownTimer <= 0)) return;

            // Calculate jump velocity (v = sqrt(2gh)).
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            didJumpThisFrame = true;
            jumpCooldownTimer = jumpCooldown;
        }

        private void HandleMovement()
        {
            // Get input.
            var inputX = Input.GetAxisRaw("Horizontal");
            var inputZ = Input.GetAxisRaw("Vertical");

            // Calculate move direction relative to player orientation.
            var forward = transform.forward * inputZ;
            var right = transform.right * inputX;
            var inputDirection = (forward + right).normalized;

            // Determine target speed based on sprint input.
            var isSprinting = Input.GetKey(KeyCode.LeftShift) && inputZ > 0;
            var maxSpeed = isSprinting ? sprintSpeed : walkSpeed;

            // If there's input, set target speed, otherwise target is 0.
            targetSpeed = inputDirection.magnitude > 0.1f ? maxSpeed : 0f;

            // Choose appropriate smoothing time based on accelerating, decelerating, or sprinting.
            float smoothTime;
            if (targetSpeed > currentSpeed) smoothTime = accelerationTime;
            else if (targetSpeed < currentSpeed && targetSpeed > 0) smoothTime = sprintTransitionTime;
            else smoothTime = decelerationTime;

            // Smoothly adjust current speed.
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

            // Calculate sprint percentage for FMOD parameter.
            currentSpeedPercent = Mathf.InverseLerp(0, sprintSpeed, currentSpeed);

            // Set move direction (keep direction even when stopping).
            if (inputDirection.magnitude > 0.1f) moveDirection = inputDirection;

            // Compute horizontal movement.
            var velocity = moveDirection * currentSpeed;

            // Apply gravity.
            if (isGrounded && verticalVelocity < 0 && !didJumpThisFrame)
            {
                verticalVelocity = -2f; // Small negative value to keep player grounded.
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            // Apply vertical movement.
            velocity.y = verticalVelocity;

            // Move the character.
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleFootsteps()
        {
            if (!isGrounded || !(currentSpeed > 0.1f)) return;

            // Calculate horizontal movement using projection on horizontal plane.
            var horizontalMovement = Vector3.ProjectOnPlane(transform.position - lastPosition, Vector3.up);
            distanceTravelled += horizontalMovement.magnitude;

            // Check if we should play a footstep.
            if (distanceTravelled >= stepLength)
            {
                var footstepSurface = GetCurrentSurface();
                if (footstepSurface != AudioSurfaceType.None)
                {
                    OnFootstepDetected?.Invoke(footstepSurface, currentSpeedPercent);
                }
                distanceTravelled -= stepLength;
            }

            lastPosition = transform.position;
        }

        private void UpdateGroundedState()
        {
            isGrounded = characterController.isGrounded;

            // Detect landing (for FMOD footstep/landing sound).
            if (isGrounded && !wasGroundedLastFrame && verticalVelocity < -2f)
            {
                // Player has just landed. (note: impact force is unused).
                var impactForce = Mathf.Abs(verticalVelocity);
            }
        }

        private AudioSurfaceType GetCurrentSurface()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out var hit, 2f, groundLayers))
            {
                // First check if we're on a tagged object.
                if (hit.collider.CompareTag("Wood"))
                {
                    return AudioSurfaceType.Wood;
                }

                // Check if we hit terrain
                if (hit.collider.GetComponent<Terrain>() != null)
                {
                    var terrain = hit.collider.GetComponent<Terrain>();

                    // Convert hit point to terrain coordinates
                    Vector3 terrainPos = hit.point - terrain.transform.position;
                    Vector2 normalizedPos = new Vector2(
                        terrainPos.x / terrain.terrainData.size.x,
                        terrainPos.z / terrain.terrainData.size.z);

                    // Sample the dominant texture.
                    var splatmapData = terrain.terrainData.GetAlphamaps(
                        Mathf.FloorToInt(normalizedPos.x * terrain.terrainData.alphamapWidth),
                        Mathf.FloorToInt(normalizedPos.y * terrain.terrainData.alphamapHeight),
                        1, 1);

                    // Find dominant texture index.
                    int dominantTexture = 0;
                    float maxMix = 0;

                    for (var i = 0; i < splatmapData.GetLength(2); i++)
                    {
                        if (splatmapData[0, 0, i] > maxMix)
                        {
                            maxMix = splatmapData[0, 0, i];
                            dominantTexture = i;
                        }
                    }

                    // Map texture index to surface type (depends on Terrain painting texture order).
                    return dominantTexture switch
                    {
                        0 => AudioSurfaceType.Dirt,  // Texture is "Grass", using "Dirt" as placeholder.
                        1 => AudioSurfaceType.Dirt,
                        2 => AudioSurfaceType.Concrete,
                        _ => AudioSurfaceType.None
                    };
                }
            }
            return AudioSurfaceType.None;
        }
    }
}