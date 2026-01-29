namespace GGJ2026.Gameplay.Character;

using FlaxEngine;

public static class InputManager
{
    public enum InputType { KeyboardMouse, Gamepad }

    static InputGamepadIndex activeGamepad = InputGamepadIndex.Gamepad1;
    const float StickDeadzone = 0.2f;
    const float TriggerDeadzone = 0.1f;
    static Vector2 smoothedAimCache = Vector2.Zero;
    static float _vibrationEndTime = 0f;
    static bool _isVibrating = false;

    public static InputType CurrentInputType { get; private set; } = InputType.KeyboardMouse;
    public static bool IsGamepadConnected { get; private set; } = false;

    public static void Initialize()
    {
        CheckGamepadConnection();
        Input.GamepadsChanged += OnGamepadsChanged;
    }

    #region Movement and Aiming Input
    public static Vector2 GetMovement()
    {
        Vector2 movement = Vector2.Zero;

        if (CurrentInputType == InputType.KeyboardMouse)
        {
            // Keyboard movement (WASD)
            movement.X = GetKeyboardHorizontal();
            movement.Y = GetKeyboardVertical();
        }
        else
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
        Vector2 aiming = Vector2.Zero;

        if (CurrentInputType == InputType.KeyboardMouse)
        {
            // For mouse aiming, we need special handling in PlayerController
            // This just returns zero - actual mouse aiming is handled in HandleMouseAiming()
            aiming = Vector2.Zero;
        }
        else
        {
            // Gamepad right stick aiming
            aiming = GetGamepadRightStick();

            // If no right stick input, aim in movement direction
            if (aiming.Length < StickDeadzone)
            {
                Vector2 move = GetMovement();
                aiming = move.Length > 0 ? move.Normalized : Vector2.Zero;
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
    public static Vector2 GetGamepadLeftStick()
    {
        var gamepad = Input.Gamepads[(int)activeGamepad];
        if (gamepad == null)
            return Vector2.Zero;

        return new Vector2(
            gamepad.GetAxis(GamepadAxis.LeftStickX),
            -gamepad.GetAxis(GamepadAxis.LeftStickY) // Invert Y
        );
    }

    public static Vector2 GetGamepadRightStick()
    {
        var gamepad = Input.Gamepads[(int)activeGamepad];
        if (gamepad == null)
            return Vector2.Zero;

        return new Vector2(
            gamepad.GetAxis(GamepadAxis.RightStickX),
            -gamepad.GetAxis(GamepadAxis.RightStickY) // Invert Y
        );
    }

    public static float GetGamepadLeftTrigger()
    {
        var gamepad = Input.Gamepads[(int)activeGamepad];
        if (gamepad == null)
            return 0f;

        var trigger = gamepad.GetAxis(GamepadAxis.LeftTrigger);
        return trigger > TriggerDeadzone ? trigger : 0f;
    }

    public static float GetGamepadRightTrigger()
    {
        var gamepad = Input.Gamepads[(int)activeGamepad];
        if (gamepad == null)
            return 0f;

        var trigger = gamepad.GetAxis(GamepadAxis.RightTrigger);
        return trigger > TriggerDeadzone ? trigger : 0f;
    }

    public static bool GetGamepadButton(GamepadButton button)
    {
        var gamepad = Input.Gamepads[(int)activeGamepad];
        return gamepad != null && gamepad.GetButton(button);
    }

    public static bool GetGamepadButtonDown(GamepadButton button)
    {
        var gamepad = Input.Gamepads[(int)activeGamepad];
        return gamepad != null && gamepad.GetButtonDown(button);
    }
    #endregion

    #region Keyboard Methods
    public static float GetKeyboardHorizontal()
    {
        float value = 0f;
        if (Input.GetKey(KeyboardKeys.D) || Input.GetKey(KeyboardKeys.ArrowRight)) value += 1f;
        if (Input.GetKey(KeyboardKeys.A) || Input.GetKey(KeyboardKeys.ArrowLeft)) value -= 1f;
        return value;
    }

    public static float GetKeyboardVertical()
    {
        float value = 0f;
        if (Input.GetKey(KeyboardKeys.W) || Input.GetKey(KeyboardKeys.ArrowUp)) value += 1f;
        if (Input.GetKey(KeyboardKeys.S) || Input.GetKey(KeyboardKeys.ArrowDown)) value -= 1f;
        return value;
    }

    public static float GetKeyboardAimHorizontal()
    {
        float value = 0f;
        if (Input.GetKey(KeyboardKeys.L) || Input.GetKey(KeyboardKeys.ArrowRight)) value += 1f;
        if (Input.GetKey(KeyboardKeys.J) || Input.GetKey(KeyboardKeys.ArrowLeft)) value -= 1f;
        return value;
    }

    public static float GetKeyboardAimVertical()
    {
        float value = 0f;
        if (Input.GetKey(KeyboardKeys.I) || Input.GetKey(KeyboardKeys.ArrowUp)) value += 1f;
        if (Input.GetKey(KeyboardKeys.K) || Input.GetKey(KeyboardKeys.ArrowDown)) value -= 1f;
        return value;
    }
    #endregion

    #region Action Buttons
    public static bool GetReload()
    {
        bool keyboardReload = Input.GetKey(KeyboardKeys.R);
        bool gamepadReload = GetGamepadButton(GamepadButton.Y);
        return keyboardReload || gamepadReload;
    }

    public static bool GetFire()
    {
        bool keyboardFire = Input.GetMouseButton(MouseButton.Left) ||
                           Input.GetKey(KeyboardKeys.Spacebar);
        bool gamepadFire = GetGamepadRightTrigger() > 0.5f ||
                          GetGamepadButton(GamepadButton.RightShoulder) ||
                          GetGamepadButton(GamepadButton.A);
        return keyboardFire || gamepadFire;
    }

    public static bool GetFireDown()
    {
        bool keyboardFire = Input.GetMouseButtonDown(MouseButton.Left) ||
                           Input.GetKeyDown(KeyboardKeys.Spacebar);
        bool gamepadFire = GetGamepadButtonDown(GamepadButton.RightShoulder) ||
                          GetGamepadButtonDown(GamepadButton.A);
        return keyboardFire || gamepadFire;
    }

    public static bool GetDash()
    {
        bool keyboardDash = Input.GetKey(KeyboardKeys.Shift);
        bool gamepadDash = GetGamepadButton(GamepadButton.LeftShoulder) ||
                          GetGamepadButton(GamepadButton.X);
        return keyboardDash || gamepadDash;
    }

    public static bool GetDashDown()
    {
        bool keyboardDash = Input.GetKeyDown(KeyboardKeys.Shift);
        bool gamepadDash = GetGamepadButtonDown(GamepadButton.LeftShoulder) ||
                          GetGamepadButtonDown(GamepadButton.X);
        return keyboardDash || gamepadDash;
    }

    public static bool GetJump()
    {
        bool keyboardJump = Input.GetKey(KeyboardKeys.Spacebar);
        bool gamepadJump = GetGamepadButton(GamepadButton.A);
        return keyboardJump || gamepadJump;
    }

    public static bool GetJumpDown()
    {
        bool keyboardJump = Input.GetKeyDown(KeyboardKeys.Spacebar);
        bool gamepadJump = GetGamepadButtonDown(GamepadButton.A);
        return keyboardJump || gamepadJump;
    }
    #endregion

    #region Utility Methods
    static void CheckGamepadConnection()
    {
        // Check all gamepad indices
        for (int i = 0; i < Input.Gamepads.Length; i++)
        {
            var gamepad = Input.Gamepads[i];
            if (gamepad != null)
            {
                IsGamepadConnected = true;
                activeGamepad = (InputGamepadIndex)i;
                return;
            }
        }
        IsGamepadConnected = false;
    }

    static void OnGamepadsChanged()
    {
        bool wasConnected = IsGamepadConnected;
        CheckGamepadConnection();

        if (wasConnected && !IsGamepadConnected && CurrentInputType == InputType.Gamepad)
        {
            CurrentInputType = InputType.KeyboardMouse;
        }
    }

    public static void DetectInputType()
    {
        // Check for any gamepad input
        bool gamepadInput = false;

        if (IsGamepadConnected)
        {
            var gamepad = Input.Gamepads[(int)activeGamepad];
            if (gamepad != null)
            {
                Vector2 leftStick = GetGamepadLeftStick();
                Vector2 rightStick = GetGamepadRightStick();

                // Check if any button is pressed or sticks are moved
                gamepadInput = leftStick.Length > StickDeadzone ||
                              rightStick.Length > StickDeadzone ||
                              gamepad.GetButton(GamepadButton.MAX);
            }
        }

        // Check for keyboard/mouse input
        bool keyboardMouseInput =
            Input.GetKeyDown(KeyboardKeys.MAX) ||
            Input.GetMouseButtonDown(MouseButton.MAX) ||
            Input.MousePositionDelta.Length > 0.01f;

        // Update input type (prefer gamepad if both inputs)
        if (gamepadInput)
            CurrentInputType = InputType.Gamepad;
        else if (keyboardMouseInput)
            CurrentInputType = InputType.KeyboardMouse;
    }

    private static Vector2 ApplyCircularDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.Length;

        if (magnitude < deadzone)
            return Vector2.Zero;

        // Normalize and rescale to 0-1 range
        float normalizedMagnitude = (magnitude - deadzone) / (1f - deadzone);
        return input.Normalized * normalizedMagnitude;
    }
    #endregion

    #region Vibration and Smoothing
    public static void SetVibration(float leftMotor, float rightMotor, float duration = 0.1f)
    {
        if (!IsGamepadConnected) return;

        var gamepad = Input.Gamepads[(int)activeGamepad];
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
                var gamepad2 = Input.Gamepads[(int)activeGamepad];
                gamepad2?.SetVibration(GamepadVibrationState.Default);
            }
        });
    }

    public static Vector2 GetSmoothedAiming(float smoothingFactor = 0.8f)
    {
        Vector2 rawAim = GetAiming();

        if (rawAim.Length > StickDeadzone)
        {
            smoothedAimCache = Vector2.Lerp(
                smoothedAimCache,
                rawAim,
                Mathf.Clamp(smoothingFactor * Time.DeltaTime * 10f, 0f, 1f)
            );
        }
        else
        {
            // Decay when no input
            smoothedAimCache = Vector2.Lerp(
                smoothedAimCache,
                Vector2.Zero,
                Mathf.Clamp(smoothingFactor * Time.DeltaTime * 5f, 0f, 1f)
            );
        }

        return smoothedAimCache;
    }
    #endregion
}