using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class Combat : MonoBehaviour
{
    private const float NoTargetDistance = -1f;
    private const int MaxTargetResults = 32;



    [Header("Targeting")] 
    public Transform CurrentTarget;

    [SerializeField] private float _targetAcquisitionRadius = 6f;
    [SerializeField] private LayerMask _targetLayers = ~0;
    [SerializeField] private float _targetRefreshInterval = 0.25f;
    
    [Header("Weapon IK")]
    public Transform WeaponMaster;
    public Transform Chest;


    private readonly Collider[] _targetResults = new Collider[MaxTargetResults];
    private CombatTarget _currentCombatTarget;
    private float _targetRefreshTimer;
    private float _currentTargetDistance = NoTargetDistance;

    public CombatTarget CurrentCombatTarget => _currentCombatTarget;
    

    private void Start()
    {
        // WeaponMaster.SetParent(Chest, false);
    }

    private void Update()
    {
        _targetRefreshTimer -= Time.deltaTime;
        bool hasAssignedTarget = CurrentTarget != null;
        bool hasInvalidTarget = hasAssignedTarget && !ValidateCurrentTarget();

        if (hasInvalidTarget || _targetRefreshTimer <= 0f)
        {
            AcquireNearestTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }

        UpdateTargetDistance();
    }



    private bool AcquireNearestTarget()
    {
        CombatTarget nearestTarget = FindNearestTarget();
        SetCurrentTarget(nearestTarget);
        return nearestTarget != null;

    }

    private void SetCurrentTarget(CombatTarget target)
    {
        _currentCombatTarget = target;
        CurrentTarget = target != null ? target.TargetPoint : null;
        UpdateTargetDistance();
    }

    private CombatTarget FindNearestTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            _targetAcquisitionRadius,
            _targetResults,
            _targetLayers,
            QueryTriggerInteraction.Ignore);

        CombatTarget nearestTarget = null;
        float nearestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _targetResults[i];
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            CombatTarget candidate = hit.GetComponentInParent<CombatTarget>();
            if (candidate == null || !candidate.IsValid)
            {
                continue;
            }

            float distanceSqr = (candidate.TargetPoint.position - transform.position).sqrMagnitude;
            if (distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }
        }

        return nearestTarget;

    }
    
    public void ClearCurrentTarget()
    {
        SetCurrentTarget(null);
    }

    private void UpdateTargetDistance()
    {
        _currentTargetDistance = CurrentTarget != null
            ? Vector3.Distance(transform.position, CurrentTarget.position)
            : NoTargetDistance;
    }
    
    private bool ValidateCurrentTarget()
    {
        if (CurrentTarget == null)
        {
            _currentCombatTarget = null;
            return false;
        }

        if (_currentCombatTarget == null)
        {
            _currentCombatTarget = CurrentTarget.GetComponentInParent<CombatTarget>();
        }

        if (_currentCombatTarget == null || !_currentCombatTarget.IsValid)
        {
            ClearCurrentTarget();
            return false;
        }

        return true;    
    }
}