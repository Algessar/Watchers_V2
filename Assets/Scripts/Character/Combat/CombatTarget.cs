using UnityEngine;

public class CombatTarget : MonoBehaviour
{
    [SerializeField] private Transform _targetPoint;
    [SerializeField] private Health _health;

    public Transform TargetPoint => _targetPoint != null ? _targetPoint : transform;
    public Health Health => _health;
    public bool IsValid => isActiveAndEnabled && (_health == null || !_health.IsDead);

    private void Awake()
    {
        CacheComponent();
    }

    private void CacheComponent()
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }
    }
}