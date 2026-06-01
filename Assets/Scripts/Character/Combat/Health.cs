using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100;
    [SerializeField] private float _currentHealth = 100f;
    [SerializeField] private bool _isInvulnerable;
    [SerializeField] private bool _isDead;

    [Header("Events")]
    [SerializeField] private UnityEvent<float, float> _onHealthChanged = new();

    [SerializeField] private UnityEvent<float> _onDamaged = new();
    [SerializeField] private UnityEvent _onDied = new();

    public event Action<float, float> HealthChanged;
    public event Action<float> Damaged;
    public event Action Died;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsVulnerable => _isInvulnerable;
    public bool IsDead => _isDead;

    private void Awake()
    {
        ClampHealth();
    }

    private void OnValidate()
    {
        ClampHealth();
    }

    void TakeDamage(float damage)
    {
        if (_isDead || _isInvulnerable || damage <= 0f)
        {
            return;
        }

        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth - damage, 0f, _maxHealth);
        float appliedDamage = previousHealth - _currentHealth;

        if (appliedDamage <= 0f) return;

        Damaged?.Invoke(appliedDamage);
        _onDamaged?.Invoke(appliedDamage);
        RaiseHealthChanged();

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;

        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);

        if (!Mathf.Approximately(previousHealth, _currentHealth))
        {
            RaiseHealthChanged();
        }
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        _isInvulnerable = isInvulnerable;
    }

    public void ResetHealth()
    {
        _isDead = false;
        _currentHealth = _maxHealth;
        RaiseHealthChanged();
    }

    private void Die()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        Died?.Invoke();
        _onDied?.Invoke();
    }

    private void RaiseHealthChanged()
    {
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        _onHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void ClampHealth()
    {
        _maxHealth = Mathf.Max(1f, _maxHealth);
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        _isDead = _currentHealth <= 0f;
    }
}
