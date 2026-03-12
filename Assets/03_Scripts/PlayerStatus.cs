using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        if (maxHealth < 1)
        {
            maxHealth = 1;
        }

        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    [ContextMenu("Restore Full Health")]
    public void RestoreFullHealth()
    {
        if (IsDead)
        {
            // Optional: remove this guard if full-heal should revive.
            return;
        }

        SetHealth(maxHealth);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return false;
        }

        SetHealth(currentHealth - amount);

        if (currentHealth == 0)
        {
            OnDied?.Invoke();
        }

        return true;
    }

    public bool Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return false;
        }

        SetHealth(currentHealth + amount);
        return true;
    }

    public void Revive(int health)
    {
        if (health <= 0)
        {
            health = 1;
        }

        SetHealth(health);
    }

    private void SetHealth(int value)
    {
        var next = Mathf.Clamp(value, 0, maxHealth);
        if (next == currentHealth)
        {
            return;
        }

        currentHealth = next;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
