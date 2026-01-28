using System.Collections.Generic;
using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public static class CustomGravity
{
    static readonly List<GravitySource> sources = [];

    public static void Register(GravitySource source)
    {
        if (sources.Contains(source))
        {
            Debug.LogError("Duplicate registration of gravity source!");
            return;
        }
        sources.Add(source);
    }

    public static void Unregister(GravitySource source)
    {
        if (!sources.Remove(source))
            Debug.LogError("Unregistration of unknown gravity source!");
    }

    public static Vector3 GetGravity(Vector3 position)
    {
        var g = Vector3.Zero;
        for (var i = 0; i < sources.Count; i++)
            g += sources[i].GetGravity(position);
        return g;
    }

    public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
    {
        var g = Vector3.Zero;
        for (var i = 0; i < sources.Count; i++)
            g += sources[i].GetGravity(position);
        upAxis = -Vector3.Normalize(g);
        return g;
    }

    public static Vector3 GetUpAxis(Vector3 position)
    {
        var g = Vector3.Zero;
        for (var i = 0; i < sources.Count; i++)
            g += sources[i].GetGravity(position);

        // Return up vector (opposite of gravity direction)
        // If no gravity sources, return world up
        if (g.LengthSquared < 0001f)
            return Vector3.Up;

        return -Vector3.Normalize(g);
    }

    public static Vector3 GetGravityDirection(Vector3 position)
    {
        var g = GetGravity(position);
        if (g.LengthSquared < 0001f)
            return Vector3.Down;
        return Vector3.Normalize(g);
    }
}