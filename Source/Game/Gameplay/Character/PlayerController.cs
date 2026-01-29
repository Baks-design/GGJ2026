namespace GGJ2026.Gameplay.Character;

using System;
using System.Timers;
using FlaxEngine;

public class PlayerController : Script
{
    [Header("General Settings")]
    [Serialize, ShowInEditor] CharacterController controller;
    [Serialize, ShowInEditor] AnimatedModel playerModel;
    [Serialize, ShowInEditor] Actor cameraPivot;
    [Serialize, ShowInEditor] Camera mainCamera;

    [Header("Input Settings")]
    [Serialize, ShowInEditor] float movementSmoothing = 0.1f;
    [Serialize, ShowInEditor] float aimSmoothing = 0.2f;
    [Serialize, ShowInEditor] float gamepadDeadzone = 0.2f;

    [Header("Mouse Settings")]
    [Serialize, ShowInEditor] float mouseCameraSpeed = 5f;
    [Serialize, ShowInEditor] float mouseCameraHeight = 5f;
    [Serialize, ShowInEditor] float mouseCameraDistance = 5f;

    [Header("Gamepad Settings")]
    [Serialize, ShowInEditor] float gamepadAimSensitivity = 2f;
    [Serialize, ShowInEditor] float gamepadMovementSensitivity = 1.5f;
    [Serialize, ShowInEditor] float aimAssistStrength = 0.3f;
    [Serialize, ShowInEditor] float aimSnapThreshold = 0.8f;
    [Serialize, ShowInEditor] float gamepadCameraDistance = 8f;
    [Serialize, ShowInEditor] float gamepadCameraHeight = 4f;
    [Serialize, ShowInEditor] float gamepadCameraFollowSpeed = 8f;

    [Header("Movement Settings")]
    [Serialize, ShowInEditor] float gravityMultiplier = 2f;
    [Serialize, ShowInEditor] float moveSpeed = 5f;
    [Serialize, ShowInEditor] float rotationSpeed = 10f;
    [Serialize, ShowInEditor] float jumpForce = 7f;

    [Header("Dash Settings")]
    [Serialize, ShowInEditor] float dashSpeed = 15f;
    [Serialize, ShowInEditor] float dashDuration = 0.2f;
    [Serialize, ShowInEditor] float dashCooldown = 1f;
    [Serialize, ShowInEditor] ParticleSystem dashEffect;

    [Header("Combat Settings")]
    [Serialize, ShowInEditor] Prefab projectilePrefab;
    [Serialize, ShowInEditor] Actor weaponPivot;
    [Serialize, ShowInEditor] Actor[] muzzlePoints;
    [Serialize, ShowInEditor] ParticleSystem muzzleFlash;
    [Serialize, ShowInEditor] AudioSource shootSound;
    [Serialize, ShowInEditor] bool AutomaticFire = true;
    [Serialize, ShowInEditor] float fireRate = 0.15f;
    [Serialize, ShowInEditor] float projectileSpeed = 30f;
    [Serialize, ShowInEditor] int damagePerShot = 10;
    [Header("Reload Settings")]
    [Serialize, ShowInEditor] float reloadTime = 1.5f;
    [Serialize, ShowInEditor] int maxAmmo = 30;
    [Serialize, ShowInEditor] int magazineSize = 10;
    [Serialize, ShowInEditor] AudioSource reloadSound;
    [Serialize, ShowInEditor] ParticleSystem reloadEffect;

    AnimGraphParameter paramMoveSpeed;
    AnimGraphParameter paramIsShooting;
    AnimGraphParameter paramIsGrounded;
    AnimGraphParameter paramVerticalVelocity;
    AnimGraphParameter paramDash;

    Vector3 velocity;
    Vector3 moveDirection;
    Vector3 aimDirection = Vector3.Forward;
    Vector3 dashDirection;
    Vector2 smoothedMovement;
    Vector3 lastAimDirection = Vector3.Forward;
    Actor nearestEnemy = null;

    float nextFireTime;
    float dashCooldownTimer;
    float dashTimer;
    int currentMuzzle;
    bool aimAssistActive = false;
    bool isGrounded;
    bool isShooting;
    bool isDashing;
    // Add these to your existing state variables
    int currentAmmo;
    int currentMagazine;
    bool isReloading = false;
    float reloadTimer = 0f;
    AnimGraphParameter paramIsReloading;

    public override void OnStart()
    {
        ValidateComponents();
        SetAnimationsParams();
        SetCursor();
        InputManager.Initialize();

        // Initialize ammo
        currentAmmo = maxAmmo;
        currentMagazine = magazineSize;
    }

    void ValidateComponents()
    {
        mainCamera = Camera.MainCamera;

        if (controller == null)
            controller = Actor.As<CharacterController>();

        if (cameraPivot == null)
        {
            cameraPivot = new EmptyActor
            {
                Name = "CameraPivot",
                Parent = Actor,
                LocalPosition = new Vector3(0f, 2f, 0f)
            };
        }

        if (weaponPivot == null && playerModel != null)
        {
            weaponPivot = new EmptyActor
            {
                Name = "WeaponPivot",
                Parent = playerModel,
                LocalPosition = new Vector3(0f, 1.5f, 0f)
            };
        }
    }

    void SetAnimationsParams()
    {
        if (playerModel == null || playerModel.AnimationGraph == null)
            return;

        paramMoveSpeed = playerModel.GetParameter("MoveSpeed");
        paramIsShooting = playerModel.GetParameter("IsShooting");
        paramIsGrounded = playerModel.GetParameter("IsGrounded");
        paramVerticalVelocity = playerModel.GetParameter("VerticalVelocity");
        paramDash = playerModel.GetParameter("Dash");
        paramIsReloading = playerModel.GetParameter("IsReloading"); // Add this
    }

    void SetCursor()
    {
        Screen.CursorVisible = false;
        Screen.CursorLock = CursorLockMode.Locked;
    }

    public override void OnUpdate()
    {
        InputManager.DetectInputType();
        HandleInput();
        UpdateReload();
        HandleCombat();
        HandleDash();
        UpdateCamera();
        UpdateAnimations();
        UpdateAimAssist();
    }

    void HandleInput()
    {
        var rawMovement = InputManager.GetMovement();
        smoothedMovement = Vector2.Lerp(
            smoothedMovement,
            rawMovement,
            movementSmoothing * Time.DeltaTime * 20f
        );

        if (mainCamera == null) return;

        var cameraForward = mainCamera.Transform.Forward;
        var cameraRight = mainCamera.Transform.Right;
        cameraForward.Y = 0f;
        cameraRight.Y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = (cameraForward * smoothedMovement.Y + cameraRight * smoothedMovement.X).Normalized;

        if (InputManager.CurrentInputType == InputManager.InputType.Gamepad)
            moveDirection *= gamepadMovementSensitivity;

        var aimInput = InputManager.GetSmoothedAiming(aimSmoothing);

        if (InputManager.CurrentInputType == InputManager.InputType.KeyboardMouse)
            HandleMouseAiming();
        else
            HandleGamepadAiming(aimInput);

        if (InputManager.GetJumpDown() && isGrounded)
        {
            velocity.Y = jumpForce;
            InputManager.SetVibration(0.3f, 0.1f, 0.2f);
        }

        if (InputManager.GetDashDown() && dashCooldownTimer <= 0f && moveDirection.Length > 0.1f)
        {
            StartDash();
            InputManager.SetVibration(0.5f, 0.5f, 0.3f);
        }

        // Shooting - only if not reloading and has ammo
        if (!isReloading && currentMagazine > 0)
        {
            isShooting = AutomaticFire ? InputManager.GetFire() : InputManager.GetFireDown();
        }
        else
        {
            isShooting = false;
        }

        // Reload input - check if player pressed reload or tried to shoot with empty magazine
        bool wantsToReload = InputManager.GetReload() ||
                            (InputManager.GetFireDown() && currentMagazine == 0 && currentAmmo > 0);
        if (wantsToReload && !isReloading && currentAmmo > 0 && currentMagazine < magazineSize)
        {
            StartReload();
        }
    }

    void HandleMouseAiming()
    {
        if (mainCamera == null) return;

        var mousePos = Input.MousePosition;
        var ray = mainCamera.ConvertMouseToRay(mousePos);
        var groundPlane = new Plane(Vector3.Up, Actor.Position);

        if (groundPlane.Intersects(ref ray, out float distance))
        {
            var targetPoint = ray.GetPoint(distance);
            aimDirection = (targetPoint - Actor.Position).Normalized;
            aimDirection.Y = 0f;
            lastAimDirection = aimDirection;
        }
    }

    void HandleGamepadAiming(Vector2 aimInput)
    {
        if (mainCamera == null) return;

        if (aimInput.Length > gamepadDeadzone)
        {
            // Convert gamepad input to world space
            var cameraForward = mainCamera.Transform.Forward;
            var cameraRight = mainCamera.Transform.Right;
            cameraForward.Y = 0f;
            cameraRight.Y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Apply aim assist if active
            if (aimAssistActive && nearestEnemy != null)
            {
                var toEnemy = (nearestEnemy.Position - Actor.Position).Normalized;
                toEnemy.Y = 0f;

                // Blend between stick input and aim assist
                var stickDirection = (cameraForward * aimInput.Y + cameraRight * aimInput.X).Normalized;
                aimDirection = Vector3.Lerp(stickDirection, toEnemy, aimAssistStrength).Normalized;
            }
            else
            {
                aimDirection = (cameraForward * aimInput.Y + cameraRight * aimInput.X).Normalized;
            }

            lastAimDirection = aimDirection;

            // Apply sensitivity
            aimDirection *= gamepadAimSensitivity;
        }
        else
        {
            // Maintain last aim direction when no input (for gamepad)
            aimDirection = lastAimDirection;
        }
    }

    void UpdateAimAssist()
    {
        // Find nearest enemy for aim assist
        var enemies = Level.FindActors(Tag.Default, true);
        var nearestDistance = float.MaxValue;
        nearestEnemy = null;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            var toEnemy = enemy.Position - Actor.Position;
            var distance = toEnemy.Length;

            // Check if enemy is in front of player
            var directionToEnemy = toEnemy.Normalized;
            var dot = Vector3.Dot(aimDirection, directionToEnemy);

            if (distance < 10f && dot > aimSnapThreshold)
            {
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        aimAssistActive = nearestEnemy != null;
    }

    void StartReload()
    {
        isReloading = true;
        reloadTimer = reloadTime;

        // Play reload sound
        reloadSound?.Play();

        // Play reload effect
        reloadEffect?.Spawn(Actor.Position);

        // Set animation parameter
        if (paramIsReloading != null)
            paramIsReloading.Value = true;

        // Gamepad vibration for reload
        if (InputManager.CurrentInputType == InputManager.InputType.Gamepad)
            InputManager.SetVibration(0.2f, 0.1f, 0.3f);
    }

    void CompleteReload()
    {
        int ammoNeeded = magazineSize - currentMagazine;
        int ammoToLoad = Math.Min(ammoNeeded, currentAmmo);

        currentMagazine += ammoToLoad;
        currentAmmo -= ammoToLoad;

        isReloading = false;

        // Reset animation parameter
        if (paramIsReloading != null)
            paramIsReloading.Value = false;

        Debug.Log($"Reloaded! Magazine: {currentMagazine}/{magazineSize}, Ammo: {currentAmmo}/{maxAmmo}");
    }

    void UpdateReload()
    {
        if (isReloading)
        {
            reloadTimer -= Time.DeltaTime;
            if (reloadTimer <= 0f)
            {
                CompleteReload();
            }
        }
    }

    void HandleCombat()
    {
        if (isShooting && Time.GameTime >= nextFireTime)
        {
            Shoot();
            nextFireTime = (float)Time.GameTime + fireRate;

            // Gamepad vibration on shoot
            if (InputManager.CurrentInputType == InputManager.InputType.Gamepad)
                InputManager.SetVibration(0.2f, 0.4f, 0.1f);
        }
    }
    void Shoot()
    {
        if (currentMagazine <= 0) return;

        var muzzle = muzzlePoints[currentMuzzle];

        var projectile = PrefabManager.SpawnPrefab(
            projectilePrefab,
            muzzle.Position,
            muzzle.Orientation
        );

        if (projectile != null)
        {
            projectile.TryGetScript<Projectile>(out var projectileScript);
            projectileScript?.Initialize(damagePerShot, projectileSpeed, Actor);
        }

        if (muzzleFlash != null)
        {
            var flash = muzzleFlash.Spawn(muzzle.Position, muzzle.Orientation);
            flash.Parent = muzzle;
        }

        shootSound?.Play();

        // Consume ammo
        currentMagazine--;

        // Cycle muzzle points
        currentMuzzle = (currentMuzzle + 1) % muzzlePoints.Length;

        // Check if magazine is empty
        if (currentMagazine == 0 && currentAmmo > 0)
        {
            // Auto-reload if ammo is available
            StartReload();
        }
    }

    void HandleDash()
    {
        if (isDashing)
        {
            dashTimer -= Time.DeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashCooldownTimer = dashCooldown;
            }
        }
        else if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.DeltaTime;
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = moveDirection;

        dashEffect?.Spawn(Actor.Position);

        if (paramDash != null)
            paramDash.Value = true;
    }

    void UpdateCamera()
    {
        if (mainCamera == null || cameraPivot == null) return;

        // Adjust camera based on input type
        var distance = InputManager.CurrentInputType == InputManager.InputType.Gamepad
            ? gamepadCameraDistance
            : mouseCameraDistance;

        var height = InputManager.CurrentInputType == InputManager.InputType.Gamepad
            ? gamepadCameraHeight
            : mouseCameraHeight;

        var speed = InputManager.CurrentInputType == InputManager.InputType.Gamepad
            ? gamepadCameraFollowSpeed
            : mouseCameraSpeed;

        // Update camera pivot
        cameraPivot.Position = Vector3.Lerp(
            cameraPivot.Position,
            Actor.Position + new Vector3(0f, 2f, 0f),
            speed * Time.DeltaTime
        );

        // Calculate camera position with gamepad-specific offset
        var cameraOffset = -aimDirection * distance + Vector3.Up * height;

        // Add slight lead for gamepad (predictive camera)
        if (InputManager.CurrentInputType == InputManager.InputType.Gamepad && moveDirection.Length > 0.1f)
            cameraOffset += moveDirection * 2f;

        var targetPosition = cameraPivot.Position + cameraOffset;
        mainCamera.Position = Vector3.Lerp(
            mainCamera.Position,
            targetPosition,
            speed * Time.DeltaTime
        );

        // Look at player with height offset
        var lookTarget = Actor.Position + new Vector3(0f, 1.5f, 0f);
        mainCamera.LookAt(lookTarget);
    }

    void UpdateAnimations()
    {
        if (paramMoveSpeed != null)
            paramMoveSpeed.Value = moveDirection.Length;

        if (paramIsShooting != null)
            paramIsShooting.Value = isShooting;

        if (paramIsGrounded != null)
            paramIsGrounded.Value = isGrounded;

        if (paramVerticalVelocity != null)
            paramVerticalVelocity.Value = velocity.Y;

        if (paramIsReloading != null)
            paramIsReloading.Value = isReloading;
    }

    public override void OnFixedUpdate()
    {
        HandleMovement();
        HandleGravity();
    }

    void HandleMovement()
    {
        if (controller == null) return;

        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.DeltaTime);
        }
        else if (moveDirection.Length > 0.1f)
        {
            var movement = moveDirection * moveSpeed * Time.DeltaTime;
            controller.Move(movement);

            if (playerModel != null)
            {
                var targetRotation = Quaternion.LookRotation(moveDirection);
                playerModel.Orientation = Quaternion.Slerp(
                    playerModel.Orientation,
                    targetRotation,
                    rotationSpeed * Time.DeltaTime
                );
            }
        }

        controller.Move(velocity * Time.DeltaTime);
    }

    void HandleGravity()
    {
        if (controller == null) return;

        if (controller.IsGrounded)
        {
            isGrounded = true;
            velocity.Y = -2f;
        }
        else
        {
            isGrounded = false;
            velocity.Y -= Mathf.Abs(Physics.Gravity.Y * gravityMultiplier) * Time.DeltaTime;
        }
    }

    public override void OnDebugDraw()
    {
        // Draw aim assist visualization
        if (aimAssistActive && nearestEnemy != null)
        {
            DebugDraw.DrawLine(Actor.Position + Vector3.Up, nearestEnemy.Position, Color.Yellow);
            DebugDraw.DrawWireSphere(new BoundingSphere(nearestEnemy.Position, 1f), Color.Yellow);
        }
    }
}
