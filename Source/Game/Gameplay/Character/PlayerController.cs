using System;
using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public class PlayerController : Script
{
    [Header("General Settings")]
    [Serialize, ShowInEditor] CharacterController controller;
    [Serialize, ShowInEditor] Actor playerModel;
    [Serialize, ShowInEditor] Actor cameraPivot;
    [Serialize, ShowInEditor] Camera mainCamera;
    [Header("Aiming Settings")]
    [Serialize, ShowInEditor] LayersMask targetsAims;
    [Serialize, ShowInEditor] float targetAimCheckRadius = 10f;
    [Serialize, ShowInEditor] float aimSmoothing = 0.2f;
    [Serialize, ShowInEditor] float mouseCameraSpeed = 5f;
    [Serialize, ShowInEditor] float mouseCameraHeight = 5f;
    [Serialize, ShowInEditor] float mouseCameraDistance = 5f;
    [Serialize, ShowInEditor] float gamepadDeadzone = 0.2f;
    [Serialize, ShowInEditor] float gamepadAimSensitivity = 2f;
    [Serialize, ShowInEditor] float gamepadMovementSensitivity = 1.5f;
    [Serialize, ShowInEditor] float aimAssistStrength = 0.3f;
    [Serialize, ShowInEditor] float aimSnapThreshold = 0.8f;
    [Serialize, ShowInEditor] float gamepadCameraDistance = 8f;
    [Serialize, ShowInEditor] float gamepadCameraHeight = 4f;
    [Serialize, ShowInEditor] float gamepadCameraFollowSpeed = 8f;
    [Header("Movement Settings")]
    [Serialize, ShowInEditor] float movementSmoothing = 40f;
    [Serialize, ShowInEditor] float gravityMultiplier = 2f;
    [Serialize, ShowInEditor] float moveSpeed = 5f;
    [Serialize, ShowInEditor] float rotationSpeed = 10f;
    [Serialize, ShowInEditor] float jumpForce = 7f;
    [Serialize, ShowInEditor] float dashSpeed = 15f;
    [Serialize, ShowInEditor] float dashDuration = 0.2f;
    [Serialize, ShowInEditor] float dashCooldown = 1f;
    //[Serialize, ShowInEditor] ParticleSystem dashEffect;
    [Header("Combat Settings")]
    [Serialize, ShowInEditor] Prefab projectilePrefab;
    [Serialize, ShowInEditor] Actor weaponPivot;
    [Serialize, ShowInEditor] Actor[] muzzlePoints;
    //[Serialize, ShowInEditor] ParticleSystem muzzleFlash;
    //[Serialize, ShowInEditor] AudioSource shootSound;
    [Serialize, ShowInEditor] bool automaticFire = true;
    [Serialize, ShowInEditor] float fireRate = 0.15f;
    [Serialize, ShowInEditor] float projectileSpeed = 30f;
    [Serialize, ShowInEditor] int damagePerShot = 10;
    [Header("Reload Settings")]
    [Serialize, ShowInEditor] float reloadTime = 1.5f;
    [Serialize, ShowInEditor] int maxAmmo = 30;
    [Serialize, ShowInEditor] int magazineSize = 10;
    //[Serialize, ShowInEditor] AudioSource reloadSound;
    //[Serialize, ShowInEditor] ParticleSystem reloadEffect;
    Actor nearestEnemy = null;
    // AnimGraphParameter paramMoveSpeed;
    // AnimGraphParameter paramIsShooting;
    // AnimGraphParameter paramIsGrounded;
    // AnimGraphParameter paramVerticalVelocity;
    // AnimGraphParameter paramDash;
    // AnimGraphParameter paramIsReloading;
    Vector3 moveDirection;
    Vector3 aimDirection = Vector3.Forward;
    Vector3 dashDirection;
    Vector3 lastAimDirection = Vector3.Forward;
    Vector2 smoothedMovement;
    float nextFireTime;
    float dashCooldownTimer;
    float dashTimer;
    float reloadTimer = 0f;
    int currentMuzzle;
    int currentAmmo;
    int currentMagazine;
    bool isShooting;
    bool isDashing;
    bool aimAssistActive = false;
    bool isReloading = false;
    Vector3 finalMoveVector;

    public override void OnEnable() => InputManager.Initialize();

    public override void OnStart()
    {
        ValidateComponents();
        SetCursor();
        AssignedValues();
        //SetAnimationsParams();
    }

    void ValidateComponents()
    {
        if (mainCamera == null)
            mainCamera = Camera.MainCamera;

        if (controller == null)
            controller = Actor.As<CharacterController>();

        if (cameraPivot == null && controller != null)
            cameraPivot = new EmptyActor
            {
                Name = "CameraPivot",
                Parent = controller,
                LocalPosition = new Vector3(0f, 2f, 0f)
            };

        if (weaponPivot == null && playerModel != null)
            weaponPivot = new EmptyActor
            {
                Name = "WeaponPivot",
                Parent = playerModel,
                LocalPosition = new Vector3(0f, 1.5f, 0f)
            };
    }

    static void SetCursor()
    {
        Screen.CursorVisible = false;
        Screen.CursorLock = CursorLockMode.Locked;
    }

    void AssignedValues()
    {
        currentAmmo = maxAmmo;
        currentMagazine = magazineSize;
    }

    // void SetAnimationsParams()
    // {
    //     if (playerModel == null || playerModel.AnimationGraph == null)
    //         return;

    //     paramMoveSpeed = playerModel.GetParameter("MoveSpeed");
    //     paramIsShooting = playerModel.GetParameter("IsShooting");
    //     paramIsGrounded = playerModel.GetParameter("IsGrounded");
    //     paramVerticalVelocity = playerModel.GetParameter("VerticalVelocity");
    //     paramDash = playerModel.GetParameter("Dash");
    //     paramIsReloading = playerModel.GetParameter("IsReloading"); 
    // }

    public override void OnUpdate()
    {
        UpdateInputManager();
        HandleInput();
        HandleRotation();
        HandleAimAssist();
        HandleDash();
        HandleCombat();
        UpdateReload();
    }

    static void UpdateInputManager() => InputManager.DetectInputType();

    void HandleInput()
    {
        var rawMovement = InputManager.GetMovement().Normalized;
        var amount = movementSmoothing * Time.DeltaTime;
        smoothedMovement = Vector2.Lerp(smoothedMovement, rawMovement, amount);

        var cameraForward = mainCamera.Transform.Forward;
        cameraForward.Y = 0f;
        cameraForward.Normalize();
        var cameraRight = mainCamera.Transform.Right;
        cameraRight.Y = 0f;
        cameraRight.Normalize();

        moveDirection = (cameraForward * smoothedMovement.Y + cameraRight * smoothedMovement.X).Normalized;
        if (InputManager.CurrentInputType is InputManager.InputType.Gamepad)
            moveDirection *= gamepadMovementSensitivity;

        // var aimInput = InputManager.GetSmoothedAiming(aimSmoothing);
        // if (InputManager.CurrentInputType is InputManager.InputType.KeyboardMouse)
        //     HandleMouseAiming(aimInput);
        // else
        //     HandleGamepadAiming(aimInput);

        if (InputManager.GetJumpDown() && controller.IsGrounded)
        {
            finalMoveVector.Y = Mathf.Sqrt(jumpForce * -2f * -Mathf.Abs(Physics.Gravity.Y * gravityMultiplier));
            InputManager.SetVibration(0.3f, 0.1f, 0.2f);
        }

        if (InputManager.GetDashDown() && dashCooldownTimer <= 0f && moveDirection.Length > 0.1f)
        {
            StartDash();
            InputManager.SetVibration(0.5f, 0.5f, 0.3f);
        }

        // Shooting - only if not reloading and has ammo
        if (!isReloading && currentMagazine > 0)
            isShooting = automaticFire ? InputManager.GetFire() : InputManager.GetFireDown();
        else
            isShooting = false;

        // Reload input - check if player pressed reload or tried to shoot with empty magazine
        var wantsToReload = InputManager.GetReload() ||
                           (InputManager.GetFireDown() && currentMagazine == 0 && currentAmmo > 0);
        if (wantsToReload && !isReloading && currentAmmo > 0 && currentMagazine < magazineSize)
            StartReload();
    }

    void HandleRotation()
    {
        var targetRotation = Quaternion.LookRotation(moveDirection);
        var amount = rotationSpeed * Time.DeltaTime;
        playerModel.Orientation = Quaternion.Slerp(playerModel.Orientation, targetRotation, amount);
    }

    void HandleMouseAiming(Vector2 mousePos)
    {
        var ray = mainCamera.ConvertMouseToRay(mousePos);
        var groundPlane = new Plane(Vector3.Up, controller.Position);
        if (groundPlane.Intersects(ref ray, out float distance))
        {
            var targetPoint = ray.GetPoint(distance);
            aimDirection = (targetPoint - controller.Position).Normalized;
            aimDirection.Y = 0f;
            lastAimDirection = aimDirection;
        }
    }

    void HandleGamepadAiming(Vector2 aimInput)
    {
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
                var toEnemy = (nearestEnemy.Position - controller.Position).Normalized;
                toEnemy.Y = 0f;

                // Blend between stick input and aim assist
                var stickDirection = (cameraForward * aimInput.Y + cameraRight * aimInput.X).Normalized;
                aimDirection = Vector3.Lerp(stickDirection, toEnemy, aimAssistStrength).Normalized;
            }
            else
                aimDirection = (cameraForward * aimInput.Y + cameraRight * aimInput.X).Normalized;

            lastAimDirection = aimDirection;

            // Apply sensitivity
            aimDirection *= gamepadAimSensitivity;
        }
        else
            // Maintain last aim direction when no input (for gamepad)
            aimDirection = lastAimDirection;
    }

    void HandleAimAssist()
    {
        nearestEnemy = null;
        aimAssistActive = false;

        if (Physics.OverlapSphere(
            controller.Position,
            targetAimCheckRadius,
            out Collider[] results,
            targetsAims,
            false))
        {
            var nearestDistance = float.MaxValue;

            // Check each found collider
            foreach (var collider in results)
            {
                if (collider == null) continue;

                // Get the actor from collider
                var enemyActor = collider;
                if (enemyActor == null || enemyActor == Actor) continue;

                // Calculate direction and distance
                var toEnemy = enemyActor.Position - controller.Position;
                var distance = toEnemy.Length;
                if (distance > 0.01f)
                {
                    // Check if enemy is in front (within aim cone)
                    var directionToEnemy = toEnemy / distance;
                    var dot = Vector3.Dot(aimDirection, directionToEnemy);
                    if (dot > aimSnapThreshold && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemyActor;
                    }
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
        //reloadSound?.Play();
        // Play reload effect
        //reloadEffect?.Spawn(controller.Position);
        // Set animation parameter
        //paramIsReloading.Value = true;

        // Gamepad vibration for reload
        if (InputManager.CurrentInputType is InputManager.InputType.Gamepad)
            InputManager.SetVibration(0.2f, 0.1f, 0.3f);
    }

    void CompleteReload()
    {
        var ammoNeeded = magazineSize - currentMagazine;
        var ammoToLoad = Math.Min(ammoNeeded, currentAmmo);
        currentMagazine += ammoToLoad;
        currentAmmo -= ammoToLoad;

        isReloading = false;

        // Reset animation parameter
        //paramIsReloading.Value = false;

        Debug.Log($"Reloaded! Magazine: {currentMagazine}/{magazineSize}, Ammo: {currentAmmo}/{maxAmmo}");
    }

    void UpdateReload()
    {
        if (!isReloading) return;

        reloadTimer -= Time.DeltaTime;
        if (reloadTimer <= 0f)
            CompleteReload();
    }

    void HandleCombat()
    {
        if (!isShooting || Time.GameTime < nextFireTime) return;

        Shoot();
        nextFireTime = Time.GameTime + fireRate;

        // Gamepad vibration on shoot
        if (InputManager.CurrentInputType is InputManager.InputType.Gamepad)
            InputManager.SetVibration(0.2f, 0.4f, 0.1f);
    }

    void Shoot()
    {
        if (currentMagazine <= 0) return;

        var muzzle = muzzlePoints[currentMuzzle];

        var projectile = PrefabManager.SpawnPrefab(
            projectilePrefab, muzzle.Position, Quaternion.Identity, new Vector3(0.1f, 0.1f, 0.1f));
        projectile.TryGetScript<Projectile>(out var projectileScript);
        projectileScript?.Initialize(damagePerShot, projectileSpeed, 5f, controller);

        //var flash = muzzleFlash.Spawn(muzzle.Position, muzzle.Orientation);
        //flash.Parent = muzzle;

        //shootSound?.Play();

        currentMagazine--;
        // Cycle muzzle points
        currentMuzzle = (currentMuzzle + 1) % muzzlePoints.Length;
        // Check if magazine is empty
        if (currentMagazine == 0 && currentAmmo > 0)
            StartReload();
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
            dashCooldownTimer -= Time.DeltaTime;
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = moveDirection;
        //dashEffect?.Spawn(controller.Position);
        //paramDash.Value = true;
    }

    public override void OnLateFixedUpdate()
    {
        UpdateCamera();
        //UpdateAnimations();
    }

    void UpdateCamera()
    {
        // Check values
        var distance = InputManager.CurrentInputType is InputManager.InputType.Gamepad
            ? gamepadCameraDistance
            : mouseCameraDistance;
        var height = InputManager.CurrentInputType is InputManager.InputType.Gamepad
            ? gamepadCameraHeight
            : mouseCameraHeight;
        var speed = InputManager.CurrentInputType is InputManager.InputType.Gamepad
            ? gamepadCameraFollowSpeed
            : mouseCameraSpeed;
        var amount = speed * Time.DeltaTime;

        // Calculate camera pivot position
        var cameraTarget = controller.Position + new Vector3(0f, 2f, 0f);
        cameraPivot.Position = Vector3.Lerp(cameraPivot.Position, cameraTarget, amount);

        // Calculate camera position with gamepad-specific offset
        var cameraOffset = -aimDirection * distance + Vector3.Up * height;
        if (InputManager.CurrentInputType is InputManager.InputType.Gamepad && moveDirection.Length > 0.1f)
            cameraOffset += moveDirection * 2f;

        // Set position to camera
        var targetPosition = cameraPivot.Position + cameraOffset;
        mainCamera.Position = Vector3.Lerp(mainCamera.Position, targetPosition, amount);
    }

    // void UpdateAnimations()
    // {
    //     paramMoveSpeed.Value = moveDirection.Length;
    //     paramIsShooting.Value = isShooting;
    //     paramIsGrounded.Value = isGrounded;
    //     paramVerticalVelocity.Value = velocity.Y;
    //     paramIsReloading.Value = isReloading;
    // }

    public override void OnFixedUpdate()
    {
        HandleMovement();
        HandleGravity();
        ApplyMovement();
    }

    void HandleMovement()
    {
        if (moveDirection != Vector3.Zero)
        {
            finalMoveVector.X = moveDirection.X * moveSpeed;
            finalMoveVector.Z = moveDirection.Z * moveSpeed;
            if (controller.IsGrounded)
                finalMoveVector.Y += moveDirection.Y * moveSpeed;
        }
        else if (moveDirection != Vector3.Zero && isDashing)
        {
            finalMoveVector.X = dashDirection.X * dashSpeed;
            finalMoveVector.Z = dashDirection.Z * dashSpeed;
            if (controller.IsGrounded)
                finalMoveVector.Y += dashDirection.Y * moveSpeed;
        }
    }

    void HandleGravity()
    {
        if (controller.IsGrounded && finalMoveVector.Y <= 0f)
            finalMoveVector.Y = -2f;
        else
            finalMoveVector.Y -= Mathf.Abs(Physics.Gravity.Y * gravityMultiplier) * Time.DeltaTime;
    }

    void ApplyMovement() => controller.Move(finalMoveVector * Time.DeltaTime);

    public override void OnDisable() => InputManager.Dispose();

    public override void OnDebugDraw()
    {
        DebugDraw.DrawWireSphere(new BoundingSphere(controller.Position, targetAimCheckRadius), Color.Yellow);
        if (aimAssistActive && nearestEnemy != null)
            DebugDraw.DrawLine(controller.Position + Vector3.Up, nearestEnemy.Position, Color.Red);
    }
}
