using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public static class InputManager
{
    public enum InputType { KeyboardMouse, Gamepad }
    static InputGamepadIndex activeGamepad = InputGamepadIndex.Gamepad1;
    static Vector2 smoothedAimCache = Vector2.Zero;
    const float StickDeadzone = 0.2f;
    const float TriggerDeadzone = 0.1f;

    public static InputType CurrentInputType { get; private set; } = InputType.KeyboardMouse;
    public static bool IsGamepadConnected { get; private set; } = false;

    public static void Initialize()
    {
        CheckGamepadConnection();
        Input.GamepadsChanged += OnGamepadsChanged;
    }

    public static void Dispose() => Input.GamepadsChanged -= OnGamepadsChanged;

    #region Movement and Aiming Input
    public static Vector2 GetMovement()
    {
        var movement = Vector2.Zero;

        if (CurrentInputType is InputType.KeyboardMouse)
        {
            // Keyboard movement (WASD)
            movement.X = GetKeyboardHorizontal();
            movement.Y = GetKeyboardVertical();
        }
        else if (CurrentInputType is InputType.Gamepad)
        {
            // Gamepad left stick movement
            movement = GetGamepadLeftStick();

            // Fallback to keyboard if no gamepad input
            if (movement.Length < StickDeadzone)
            {
                movement.X = GetKeyboardHorizontal();
                movement.Y = GetKeyboardVertical();
            }
        }

        return ApplyCircularDeadzone(movement, StickDeadzone);
    }

    public static Vector2 GetAiming()
    {
        var aiming = Vector2.Zero;

        if (CurrentInputType is InputType.KeyboardMouse)
            // For mouse aiming, we need special handling in PlayerController
            // This just returns zero - actual mouse aiming is handled in HandleMouseAiming()
            aiming = Input.MousePosition;
        else if (CurrentInputType is InputType.Gamepad)
        {
            // Gamepad right stick aiming
            aiming = GetGamepadRightStick();

            // If no right stick input, aim in movement direction
            if (aiming.Length < StickDeadzone)
            {
                var move = GetMovement();
                aiming = move.Length > 0f ? move.Normalized : Vector2.Zero;
            }
        }

        // Keyboard aiming fallback (IJKL)
        if (aiming.Length < StickDeadzone)
        {
            aiming.X = GetKeyboardAimHorizontal();
            aiming.Y = GetKeyboardAimVertical();
        }

        return ApplyCircularDeadzone(aiming, StickDeadzone);
    }
    #endregion

    #region Gamepad Methods
    // Helper method to safely get the active gamepad
    private static Gamepad GetActiveGamepad()
    {
        if (!IsGamepadConnected)
            return null;

        int index = (int)activeGamepad;
        if (index < 0 || index >= Input.Gamepads.Length)
            return null;

        return Input.Gamepads[index];
    }

    public static Vector2 GetGamepadLeftStick()
    {
        var gamepad = GetActiveGamepad();
        if (gamepad == null)
            return Vector2.Zero;

        return new Vector2(
            gamepad.GetAxis(GamepadAxis.LeftStickX),
            -gamepad.GetAxis(GamepadAxis.LeftStickY) // Invert Y
        );
    }

    public static Vector2 GetGamepadRightStick()
    {
        var gamepad = GetActiveGamepad();
        if (gamepad == null)
            return Vector2.Zero;

        return new Vector2(
            gamepad.GetAxis(GamepadAxis.RightStickX),
            -gamepad.GetAxis(GamepadAxis.RightStickY) // Invert Y
        );
    }

    public static float GetGamepadLeftTrigger()
    {
        var gamepad = GetActiveGamepad();
        if (gamepad == null)
            return 0f;

        var trigger = gamepad.GetAxis(GamepadAxis.LeftTrigger);
        return trigger > TriggerDeadzone ? trigger : 0f;
    }

    public static float GetGamepadRightTrigger()
    {
        var gamepad = GetActiveGamepad();
        if (gamepad == null)
            return 0f;

        var trigger = gamepad.GetAxis(GamepadAxis.RightTrigger);
        return trigger > TriggerDeadzone ? trigger : 0f;
    }

    public static bool GetGamepadButton(GamepadButton button)
    {
        var gamepad = GetActiveGamepad();
        if (gamepad == null)
            return false;

        return gamepad.GetButton(button);
    }

    public static bool GetGamepadButtonDown(GamepadButton button)
    {
        var gamepad = GetActiveGamepad();
        if (gamepad == null)
            return false;

        return gamepad.GetButtonDown(button);
    }
    #endregion

    #region Keyboard Methods
    public static float GetKeyboardHorizontal()
    {
        var value = 0f;
        if (Input.GetKey(KeyboardKeys.D) || Input.GetKey(KeyboardKeys.ArrowRight)) value += 1f;
        if (Input.GetKey(KeyboardKeys.A) || Input.GetKey(KeyboardKeys.ArrowLeft)) value -= 1f;
        return value;
    }

    public static float GetKeyboardVertical()
    {
        var value = 0f;
        if (Input.GetKey(KeyboardKeys.W) || Input.GetKey(KeyboardKeys.ArrowUp)) value += 1f;
        if (Input.GetKey(KeyboardKeys.S) || Input.GetKey(KeyboardKeys.ArrowDown)) value -= 1f;
        return value;
    }

    public static float GetKeyboardAimHorizontal()
    {
        var value = 0f;
        if (Input.GetKey(KeyboardKeys.L) || Input.GetKey(KeyboardKeys.ArrowRight)) value += 1f;
        if (Input.GetKey(KeyboardKeys.J) || Input.GetKey(KeyboardKeys.ArrowLeft)) value -= 1f;
        return value;
    }

    public static float GetKeyboardAimVertical()
    {
        var value = 0f;
        if (Input.GetKey(KeyboardKeys.I) || Input.GetKey(KeyboardKeys.ArrowUp)) value += 1f;
        if (Input.GetKey(KeyboardKeys.K) || Input.GetKey(KeyboardKeys.ArrowDown)) value -= 1f;
        return value;
    }
    #endregion

    #region Action Buttons
    public static bool GetReload()
    {
        var keyboardReload = Input.GetKey(KeyboardKeys.R);
        var gamepadReload = GetGamepadButton(GamepadButton.Y);
        return keyboardReload || gamepadReload;
    }

    public static bool GetFire()
    {
        var keyboardFire = Input.GetMouseButton(MouseButton.Left);
        var gamepadFire = GetGamepadRightTrigger() > 0.5f;
        return keyboardFire || gamepadFire;
    }

    public static bool GetFireDown()
    {
        var keyboardFire = Input.GetMouseButtonDown(MouseButton.Left);
        var gamepadFire = GetGamepadRightTrigger() > 0.5f;
        return keyboardFire || gamepadFire;
    }

    public static bool GetDash()
    {
        var keyboardDash = Input.GetKey(KeyboardKeys.Shift);
        var gamepadDash = GetGamepadButton(GamepadButton.X);
        return keyboardDash || gamepadDash;
    }

    public static bool GetDashDown()
    {
        var keyboardDash = Input.GetKeyDown(KeyboardKeys.Shift);
        var gamepadDash = GetGamepadButtonDown(GamepadButton.X);
        return keyboardDash || gamepadDash;
    }

    public static bool GetJump()
    {
        var keyboardJump = Input.GetKey(KeyboardKeys.Spacebar);
        var gamepadJump = GetGamepadButton(GamepadButton.A);
        return keyboardJump || gamepadJump;
    }

    public static bool GetJumpDown()
    {
        var keyboardJump = Input.GetKeyDown(KeyboardKeys.Spacebar);
        var gamepadJump = GetGamepadButtonDown(GamepadButton.A);
        return keyboardJump || gamepadJump;
    }
    #endregion

    #region Utility Methods
    static void CheckGamepadConnection()
    {
        // Check all gamepad indices
        for (var i = 0; i < Input.Gamepads.Length; i++)
        {
            var gamepad = Input.Gamepads[i];
            if (gamepad != null)
            {
                IsGamepadConnected = true;
                activeGamepad = (InputGamepadIndex)i;
                CurrentInputType = InputType.Gamepad;
                return;
            }
        }

        // No gamepad found
        IsGamepadConnected = false;
        CurrentInputType = InputType.KeyboardMouse;
    }

    static void OnGamepadsChanged()
    {
        var wasConnected = IsGamepadConnected;
        CheckGamepadConnection();

        if (wasConnected && !IsGamepadConnected && CurrentInputType is InputType.Gamepad)
            CurrentInputType = InputType.KeyboardMouse;
    }

    public static void DetectInputType()
    {
        // Check for any gamepad input
        var gamepadInput = false;

        if (IsGamepadConnected)
        {
            var gamepad = GetActiveGamepad();
            if (gamepad != null)
            {
                var leftStick = GetGamepadLeftStick();
                var rightStick = GetGamepadRightStick();

                // Check if any button is pressed or sticks are moved
                gamepadInput = leftStick.Length > StickDeadzone ||
                              rightStick.Length > StickDeadzone ||
                              gamepad.GetButton(GamepadButton.A) ||
                              gamepad.GetButton(GamepadButton.B) ||
                              gamepad.GetButton(GamepadButton.X) ||
                              gamepad.GetButton(GamepadButton.Y) ||
                              gamepad.GetButton(GamepadButton.LeftShoulder) ||
                              gamepad.GetButton(GamepadButton.RightShoulder) ||
                              gamepad.GetButton(GamepadButton.Start) ||
                              gamepad.GetButton(GamepadButton.Back);
            }
        }

        // Check for keyboard/mouse input
        var keyboardMouseInput =
            Input.GetKeyDown(KeyboardKeys.A) ||
            Input.GetKeyDown(KeyboardKeys.W) ||
            Input.GetKeyDown(KeyboardKeys.S) ||
            Input.GetKeyDown(KeyboardKeys.D) ||
            Input.GetMouseButtonDown(MouseButton.Left) ||
            Input.GetMouseButtonDown(MouseButton.Right) ||
            Input.GetMouseButtonDown(MouseButton.Middle) ||
            Input.MousePositionDelta.Length > 0.01f;

        // Update input type (prefer gamepad if both inputs)
        if (gamepadInput)
            CurrentInputType = InputType.Gamepad;
        else if (keyboardMouseInput)
            CurrentInputType = InputType.KeyboardMouse;
    }

    private static Vector2 ApplyCircularDeadzone(Vector2 input, float deadzone)
    {
        var magnitude = input.Length;
        if (magnitude < deadzone)
            return Vector2.Zero;

        // Normalize and rescale to 0-1 range
        var normalizedMagnitude = (magnitude - deadzone) / (1f - deadzone);
        return input.Normalized * normalizedMagnitude;
    }
    #endregion

    #region Vibration and Smoothing
    public static void SetVibration(float leftMotor, float rightMotor, float duration = 0.1f)
    {
        if (!IsGamepadConnected) return;

        var gamepad = GetActiveGamepad();
        if (gamepad == null) return;

        var vibration = new GamepadVibrationState
        {
            LeftLarge = Mathf.Clamp(leftMotor, 0f, 1f),
            RightLarge = Mathf.Clamp(rightMotor, 0f, 1f),
            LeftSmall = Mathf.Clamp(leftMotor * 0.5f, 0f, 1f),
            RightSmall = Mathf.Clamp(rightMotor * 0.5f, 0f, 1f)
        };
        gamepad.SetVibration(vibration);

        // Schedule vibration stop on next update
        var stopTime = Time.GameTime + duration;
        Scripting.InvokeOnUpdate(() =>
        {
            if (Time.GameTime >= stopTime && IsGamepadConnected)
            {
                var gamepad2 = GetActiveGamepad();
                gamepad2?.SetVibration(GamepadVibrationState.Default);
            }
        });
    }

    public static Vector2 GetSmoothedAiming(float smoothingFactor = 0.8f)
    {
        var rawAim = GetAiming();

        if (rawAim.Length > StickDeadzone)
            smoothedAimCache = Vector2.Lerp(
                smoothedAimCache,
                rawAim,
                Mathf.Clamp(smoothingFactor * Time.DeltaTime * 10f, 0f, 1f)
            );
        else
            // Decay when no input
            smoothedAimCache = Vector2.Lerp(
                smoothedAimCache,
                Vector2.Zero,
                Mathf.Clamp(smoothingFactor * Time.DeltaTime * 5f, 0f, 1f)
            );

        return smoothedAimCache;
    }
    #endregion
}