using System;
using FlaxEngine;

namespace GGJ2026.Gameplay.Character;

public class CharacterHealth : Script, IDamageable
{
    [Range(10f, 200f)] public float maxHealth = 100f;
    [Range(0f, 1f)] public float invincibilityTime = 0.5f;
    float currentHealth;
    float invincibilityTimer;
    bool isInvincible;
    bool isDying = false;

    public float CurrentHealth => currentHealth;
    public float HealthMax => maxHealth;
    public bool IsAlive => currentHealth > 0;

    public event Action<float> HealthChanged;
    public event Action Death;

    public override void OnStart() => currentHealth = maxHealth;

    public override void OnUpdate()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.DeltaTime;
            if (invincibilityTimer <= 0f)
                isInvincible = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive || isInvincible || isDying) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        HealthChanged?.Invoke(currentHealth);

        // Add invincibility frames
        if (damage > 0f)
        {
            isInvincible = true;
            invincibilityTimer = invincibilityTime;
        }

        if (currentHealth <= 0f && !isDying)
        {
            isDying = true;
            StartDeath();
        }
    }

    void StartDeath()
    {
        // Prevent multiple death calls
        if (isDying)
        {
            Death?.Invoke();
            OnDeathCompleted();
        }
    }

    void OnDeathCompleted()
    {
        isDying = false;

        if (Actor.IsActive)
            DestroyNow(Actor);
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;

        var oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (Mathf.Abs(currentHealth - oldHealth) > 0.01f)
            HealthChanged?.Invoke(currentHealth);
    }
}