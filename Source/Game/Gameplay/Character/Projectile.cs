using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public class Projectile : Script
{
    [Serialize, ShowInEditor] RigidBody rigidBody;
    [Serialize, ShowInEditor] Collider targetCollider;
    //[Serialize, ShowInEditor] Prefab impactEffect;
    [Serialize, ShowInEditor] LayersMask collisionLayers;
    Actor owner;
    int damage;
    float lifetime;
    private float speed;

    public void Initialize(int damage, float speed, float lifetime, Actor owner)
    {
        this.damage = damage;
        this.owner = owner;
        this.lifetime = lifetime;
        this.speed = speed;

        if (targetCollider == null)
            targetCollider = Actor.GetScript<Collider>();

        if (rigidBody == null)
            rigidBody = Actor.As<RigidBody>();
    }

    public override void OnEnable()
    {
        rigidBody.LinearVelocity = Actor.Direction * speed;
        targetCollider.CollisionEnter += OnCollisionEnter;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (owner != null && /*impactEffect != null*/ collision.OtherCollider == owner)
            return;

        if (collisionLayers.HasLayer(collision.OtherActor.Layer))
        {
            collision.OtherActor.TryGetScript<IDamageable>(out var other);
            other?.TakeDamage(damage);

            //PrefabManager.SpawnPrefab(impactEffect, Actor.Position, Quaternion.Identity);

            Destroy(Actor);
        }
    }

    public override void OnStart() => Destroy(Actor, lifetime);
}