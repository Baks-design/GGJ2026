using System;
using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public class Projectile : Script
{
    //[Serialize, ShowInEditor] Prefab impactEffect;
    [Serialize, ShowInEditor] Collider targetCollider;
    [Serialize, ShowInEditor] LayersMask collisionLayers;
    [Serialize, ShowInEditor] float speed = 30f;
    [Serialize, ShowInEditor] float lifetime = 3f;
    [Serialize, ShowInEditor] int damage = 10;
    [Serialize, ShowInEditor] bool isHoming = false;
    [Serialize, ShowInEditor] float homingStrength = 5f;
    readonly Actor target;
    Actor owner;
    RigidBody rigidBody;
    float spawnTime;

    public void Initialize(int damage, float speed, Actor ownerActor)
    {
        this.damage = damage;
        this.speed = speed;
        owner = ownerActor;

        spawnTime = Time.GameTime;
        rigidBody = Actor.As<RigidBody>();
        if (rigidBody != null)
            rigidBody.LinearVelocity = Actor.Direction * speed;
    }

    public override void OnEnable() => targetCollider.CollisionEnter += OnCollisionEnter;

    public override void OnDisable() => targetCollider.CollisionEnter -= OnCollisionEnter;

    void OnCollisionEnter(Collision collision)
    {
        if (owner != null && /*impactEffect != null*/  collision.OtherCollider == owner)
            return;

        if (collisionLayers.HasLayer(collision.OtherActor.Layer))
        {
            // var enemyHealth = collision.OtherActor.GetScript<EnemyHealth>();
            // enemyHealth?.TakeDamage(damage);

            // var player = collision.OtherActor.GetScript<PlayerController>();
            // if (player != null && player.Actor != owner)
            //     player.TakeDamage(damage);

            // PrefabManager.SpawnPrefab(impactEffect, Actor.Position, Quaternion.Identity);

            Destroy(Actor);
        }
    }

    public override void OnStart() => Destroy(Actor, lifetime);

    public override void OnFixedUpdate()
    {
        if (target == null || rigidBody == null) return;

        if (isHoming)
        {
            var direction = (target.Position - Actor.Position).Normalized;
            var targetRotation = Quaternion.LookRotation(direction);
            Actor.Orientation = Quaternion.Slerp(Actor.Orientation, targetRotation, homingStrength * Time.DeltaTime);

            rigidBody.LinearVelocity = Actor.Direction * speed;
        }

        // Auto-destroy if stuck
        if (Time.GameTime - spawnTime > lifetime * 0.5f && rigidBody.LinearVelocity.Length < 0.1f)
            Destroy(Actor);
    }
}