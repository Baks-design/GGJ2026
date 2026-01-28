using FlaxEngine;

namespace ElusiveLife.Application.Inputs;

public interface ICharacterMovementInput
{
    bool HasMovementInput { get; }

    Vector2 GetMovementDirection();
    Vector2 GetLookRotation();
    bool IsRightMaskHeld();
    bool IsLeftMaskHeld();
}
