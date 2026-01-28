using FlaxEngine;

namespace ElusiveLife.Application.Inputs;

public struct BufferedInput
{
    public InputType Type;
    public float Time;
    public bool BoolValue;
    public float FloatValue;
    public Vector2 Vector2Value;
}