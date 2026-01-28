using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public class GravitySource : Script
{
    public virtual Vector3 GetGravity(Vector3 position) => Physics.Gravity;

    public override void OnEnable() => CustomGravity.Register(this);

    public override void OnDisable() => CustomGravity.Unregister(this);
}
