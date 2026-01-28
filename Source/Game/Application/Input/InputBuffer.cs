using System.Collections.Generic;
using FlaxEngine;

namespace ElusiveLife.Application.Inputs;

public class InputBuffer(float bufferDuration)
{
    readonly float bufferDuration = bufferDuration;
    readonly Queue<BufferedInput> buffer = new();
    readonly Dictionary<InputType, float> lastPressTimes = [];

    public void BufferInput(
        InputType type, bool boolValue = false, float floatValue = 0f,
        Vector2 vector2Value = default)
    {
        var input = new BufferedInput
        {
            Type = type,
            Time = Time.GameTime,
            BoolValue = boolValue,
            FloatValue = floatValue,
            Vector2Value = vector2Value
        };
        buffer.Enqueue(input);
        lastPressTimes[type] = Time.GameTime;
    }

    public bool ConsumeInput(InputType type, out bool boolValue) 
        => ConsumeInput(type, out boolValue, out _, out _);

    public bool ConsumeInput(
        InputType type, out bool boolValue, out float floatValue,
        out Vector2 vector2Value)
    {
        boolValue = false;
        floatValue = 0f;
        vector2Value = Vector2.Zero;

        ClearExpired();

        // Temporary queue for inputs we need to keep
        var keepQueue = new Queue<BufferedInput>();
        var found = false;

        while (buffer.Count > 0)
        {
            var input = buffer.Dequeue();

            if (!found && input.Type == type)
            {
                // Found the first matching input
                boolValue = input.BoolValue;
                floatValue = input.FloatValue;
                vector2Value = input.Vector2Value;
                found = true;
            }
            else
                // Keep this input
                keepQueue.Enqueue(input);
        }

        // Put kept inputs back in the buffer
        while (keepQueue.Count > 0)
            buffer.Enqueue(keepQueue.Dequeue());

        return found;
    }

    public bool IsPressed(InputType type)
    {
        if (lastPressTimes.TryGetValue(type, out var lastPressTime))
            return Time.GameTime <= lastPressTime + bufferDuration;
        return false;
    }

    public void Clear(InputType type)
    {
        var newBuffer = new Queue<BufferedInput>();
        while (buffer.Count > 0)
        {
            var input = buffer.Dequeue();
            if (input.Type != type)
                newBuffer.Enqueue(input);
        }

        while (newBuffer.Count > 0)
            buffer.Enqueue(newBuffer.Dequeue());

        lastPressTimes.Remove(type);
    }

    public void ClearExpired()
    {
        while (buffer.Count > 0 &&
               Time.GameTime > buffer.Peek().Time + bufferDuration)
            buffer.Dequeue();
    }
}