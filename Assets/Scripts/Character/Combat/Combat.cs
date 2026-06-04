using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


public enum CombatTargetLockMode
{
    None,
    Soft,
    Hard
}

public class Combat : MonoBehaviour
{
    private PlayerInput _input;
    
    private const float NoTargetDistance = -1f;
    private const int MaxTargetResults = 32;



    [Header("Targeting")] 
    public Transform CurrentTarget;

    [SerializeField] private float _targetAcquisitionRadius = 6f;
    [SerializeField] private LayerMask _targetLayers = ~0;
    [SerializeField] private float _targetRefreshInterval = 0.25f;
    

    private readonly Collider[] _targetResults = new Collider[MaxTargetResults];
    private CombatTarget _currentCombatTarget;
    private float _targetRefreshTimer;
    private float _currentTargetDistance = NoTargetDistance;
    public CombatTarget CurrentCombatTarget => _currentCombatTarget;

    [Header("Locking")]
    
    private CombatTargetLockMode _targetLockMode = CombatTargetLockMode.None;
    
    [SerializeField] private bool _softLockByDefault = true;

    [SerializeField] private float _softLockBreakDistance = 8f;
    [SerializeField] private float _softLockEscapeDistance = 2.5f;
    [SerializeField, Range (0f, 1f)] private float _softLockEscapeInputThreshold = 0.65f;
    [SerializeField] private float _softLockReacquireDelay = 0.75f;
    [SerializeField] private float _lockedRotationResponsiveness = 8f;
    [SerializeField, Range(0f, 0.85f)] private float _lockRotationStrength = 0.55f; 
    
    private float _softLockCooldownTimer;
    
    // Public getters
    public CombatTargetLockMode TargetLockMode => _targetLockMode;
    public float LockRotationStrength => _lockRotationStrength;
    public bool HasCurrentTarget => CurrentTarget != null;
    public bool IsLockedOn => HasCurrentTarget && _targetLockMode != CombatTargetLockMode.None;
    
    
        
    [Header("Weapon IK")]
    public Transform WeaponMaster;
    public Transform Chest;
    
    
    private void Start()
    {
        _input = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        _targetRefreshTimer -= Time.deltaTime;
        _softLockCooldownTimer -= Time.deltaTime;
        
        if (_input != null && _input.ConsumeTargetLockTrigger())
        {
            ToggleHardLock();
        }
        
        
        bool hasAssignedTarget = CurrentTarget != null;
        bool hasInvalidTarget = hasAssignedTarget && !ValidateCurrentTarget();
        
        UpdateTargetDistance();

        if (hasInvalidTarget)
        {
            ClearCurrentTarget();
        }
        else if (_targetLockMode == CombatTargetLockMode.Soft && ShouldBreakSoftLock())
        {
            ClearCurrentTarget();
            _softLockCooldownTimer = _softLockReacquireDelay;
        }
        else if (_targetLockMode != CombatTargetLockMode.Hard && _targetRefreshTimer <= 0f)
        {
            AcquireSoftLockTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }

        UpdateTargetDistance();


    }

    private void LateUpdate()
    {
        RotateTowardLockedTarget();
    }

    private void RotateTowardLockedTarget()
    {
        if (!IsLockedOn || _lockRotationStrength <= 0)
        {
            return;
        }

        Vector3 directionToTarget = Vector3.ProjectOnPlane(CurrentTarget.position - transform.position, Vector3.up);
        if (directionToTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
        float effectiveStrength = Mathf.Clamp(_lockRotationStrength, 0f, 0.95f);
        float rotationBlend = 1f - Mathf.Exp(-_lockedRotationResponsiveness * effectiveStrength * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Min(rotationBlend, 0.95f));
    }

    private bool AcquireSoftLockTarget()
    {

        if (_softLockByDefault || _softLockCooldownTimer > 0f)
        {
            if (_targetLockMode == CombatTargetLockMode.Soft)
            {
                ClearCurrentTarget();
            }

            return false;
        }

        CombatTarget nearestTarget = FindNearestTarget();
        SetCurrentTarget(nearestTarget, nearestTarget != null ? CombatTargetLockMode.Soft : CombatTargetLockMode.None);
        return nearestTarget != null;
    }

    private bool ToggleHardLock()
    {
        if (_targetLockMode == CombatTargetLockMode.Hard)
        {
            ClearCurrentTarget();
            return false;
        }
        
        CombatTarget target = _currentCombatTarget != null && _currentCombatTarget.IsValid
            ? _currentCombatTarget
            : FindNearestTarget();

        if (target == null)
        {
            ClearCurrentTarget();
            return false;
        }

        SetCurrentTarget(target, CombatTargetLockMode.Hard);

        Debug.Log($"Toggled Hard Lock", this);
        return true;
        
    }

    private void UpdateTargetDistance()
    {
        _currentTargetDistance = CurrentTarget != null
            ? Vector3.Distance(transform.position, CurrentTarget.position)
            : NoTargetDistance;
    }
    
    public bool AcquireNearestTarget()
    {
        CombatTarget nearestTarget = FindNearestTarget();
        SetCurrentTarget(nearestTarget, nearestTarget != null ? CombatTargetLockMode.Soft : CombatTargetLockMode.None);        return nearestTarget != null;

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
            if (!hit || hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            CombatTarget candidate = hit.GetComponentInParent<CombatTarget>();
            if (!candidate || !candidate.IsValid)
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
    
    private void SetCurrentTarget(CombatTarget target, CombatTargetLockMode lockMode)
    {
        _currentCombatTarget = target;
        CurrentTarget = target != null ? target.TargetPoint : null;
        _targetLockMode = target != null ? lockMode : CombatTargetLockMode.None;
        UpdateTargetDistance();
    }
    
    public void ClearCurrentTarget()
    {
        SetCurrentTarget(null, CombatTargetLockMode.None);
    }
    
    private bool ShouldBreakSoftLock()
    {
        if (CurrentTarget == null) return false;

        if (_currentTargetDistance > _softLockBreakDistance)
        {
            return true;
        }

        if (_input == null || _input.direction.sqrMagnitude <= 0.01f ||
            _currentTargetDistance < _softLockEscapeDistance)
        {
            return false;
        }

        return _input.direction.y <= -_softLockEscapeInputThreshold;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        Gizmos.DrawWireSphere(transform.position, _targetAcquisitionRadius);
    }
}