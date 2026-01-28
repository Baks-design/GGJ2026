using FlaxEngine;

namespace ElusiveLife.Application.Inputs;

public class CharacterInputProvider : ICharacterMovementInput
{
    public bool HasMovementInput
        => GetMovementDirection().X != 0f ||
            GetMovementDirection().Y != 0f;

    public Vector2 GetMovementDirection() =>
        new(Input.GetAxis("MoveHorizontal"), Input.GetAxis("MoveVertical"));

    public Vector2 GetLookRotation() =>
        new(Input.GetAxis("LookHorizontal"), Input.GetAxis("LookVertical"));

    public float GetSwimming() => Input.GetAxis("UpDown");

    public bool IsLeftMaskHeld() => Input.GetAction("LeftMask");

    public bool IsRightMaskHeld() => Input.GetAction("RightMask");

    public bool IsJump() => Input.GetAction("Jump");

    public bool IsClimb() => Input.GetAction("Climb");
}
