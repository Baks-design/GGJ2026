using FlaxEngine;

namespace GGJ2026.Gameplay.Utils;

public static class QuaternionExtensions
{
    public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
    {
        // Normalize inputs
        var a = fromDirection.Normalized;
        var b = toDirection.Normalized;

        // Calculate dot product and check for parallel vectors
        var dot = Vector3.Dot(a, b);
        // If vectors are nearly identical, return identity
        if (dot > 0.999999f)
            return Quaternion.Identity;

        // If vectors are nearly opposite, find perpendicular axis
        if (dot < -0.999999f)
        {
            // Find any perpendicular vector to use as axis
            var axiss = Vector3.Cross(Vector3.Right, a);
            if (axiss.LengthSquared < 0.000001f)
                axiss = Vector3.Cross(Vector3.Up, a);
            axiss.Normalize();
            return AngleAxis(180f, axiss);
        }

        // Normal case: calculate rotation axis and angle
        var axis = Vector3.Cross(a, b).Normalized;
        var angle = Mathf.Acos(dot) * Mathf.RadiansToDegrees;

        return AngleAxis(angle, axis);
    }

    public static Quaternion AngleAxis(float angle, Vector3 axis)
    {
        // Normalize the axis
        axis.Normalize();

        // Convert angle to radians and halve it
        var halfAngle = angle * Mathf.DegreesToRadians * 0.5f;

        // Calculate sine and cosine
        var sinHalfAngle = Mathf.Sin(halfAngle);
        var cosHalfAngle = Mathf.Cos(halfAngle);

        // Construct quaternion
        return new Quaternion(
            axis.X * sinHalfAngle,
            axis.Y * sinHalfAngle,
            axis.Z * sinHalfAngle,
            cosHalfAngle
        );
    }

    public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
    {
        // Calculate dot product
        var cosOmega = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        // If dot product is negative, negate one quaternion to take shorter path
        if (cosOmega < 0f)
        {
            b = new Quaternion(-b.X, -b.Y, -b.Z, -b.W);
            cosOmega = -cosOmega;
        }

        // Check if quaternions are very close
        float k0, k1;
        if (cosOmega > 0.9999f)
        {
            // Very close - use linear interpolation to avoid division by sin(θ)
            k0 = 1.0f - t;
            k1 = t;
        }
        else
        {
            // Calculate sin(θ)
            var sinOmega = Mathf.Sqrt(1f - cosOmega * cosOmega);
            var omega = Mathf.Atan2(sinOmega, cosOmega);

            // Calculate interpolation coefficients
            var oneOverSinOmega = 1f / sinOmega;
            k0 = Mathf.Sin((1f - t) * omega) * oneOverSinOmega;
            k1 = Mathf.Sin(t * omega) * oneOverSinOmega;
        }

        // Interpolate and return
        return new Quaternion(
            k0 * a.X + k1 * b.X,
            k0 * a.Y + k1 * b.Y,
            k0 * a.Z + k1 * b.Z,
            k0 * a.W + k1 * b.W
        );
    }
}
