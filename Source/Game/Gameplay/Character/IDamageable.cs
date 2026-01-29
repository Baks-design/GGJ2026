namespace GGJ2026.Gameplay.Character;

public interface IDamageable
{
    float HealthMax { get; }
    float CurrentHealth { get; }
    bool IsAlive { get; }

    void TakeDamage(float damage);
}
