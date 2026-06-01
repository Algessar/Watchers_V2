using System;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Transform _ownerRoot;
    [SerializeField] private Transform _swordTip;

    [SerializeField] private Collider _tipCollider;
    // [SerializeField] private Collider _edgeTopCollider;
    // [SerializeField] private Collider _edgeMidCollider;
    // [SerializeField] private Collider _edgeHiltCollider;

    [SerializeField] private float _damage = 5;
    [SerializeField] private float _velocityDamageMultiplier = 1f;
    
    private float _totalDamage;
    private float _tipVelocity;
    private Vector3 _previousTipPosition;

    private bool _attackStarted;
    private bool _hitRegistered;
    
    [SerializeField] private bool _disableTipColliderOutsideDamageWindow = true;
    private bool _damageWindowActive;
    public bool IsDamageWindowActive => _damageWindowActive;

    private readonly HashSet<IDamageable> _damagedTargets = new();

    private void Awake()
    {
        if (_ownerRoot == null)
        {
            _ownerRoot = transform.root; //NOTE: No idea what this is for really
        }
    }
    private void Update()
    {
        UpdateTipVelocity(Time.deltaTime);   
    }

    private void OnEnable()
    {
        ResetTipTracking();
        ResetHitRegistration();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_damageWindowActive) return;
        
        ApplyDamage(other);
    }

    private void ResetTipTracking()
    {
        _previousTipPosition = GetTipPosition();
        _tipVelocity = 0f;
    }

    private Vector3 GetTipPosition()
    {
        
        return _swordTip != null ? _swordTip.position : transform.position;
    }


    private void ResetHitRegistration()
    {
        _hitRegistered = false;
        _totalDamage = 0;
        _damagedTargets.Clear();

    }

    

    public void BeginDamageWindow()
    {
        _damageWindowActive = true;
        _attackStarted = true;
        ResetTipTracking();
        ResetHitRegistration();
        SetTipColliderEnabled(true);

        if (_tipCollider != null)
        {
            _tipCollider.enabled = true;
        }
    }

    private void SetTipColliderEnabled(bool b)
    {
        if (_tipCollider != null && _disableTipColliderOutsideDamageWindow)
        {
            _tipCollider.enabled = enabled;
        }
    }

    public void EndDamageWindow()
    {
        _attackStarted = false;
        ResetHitRegistration();
        ResetTipTracking();
        
        if (_tipCollider != null)
        {
            _tipCollider.enabled = false;
        }
    }

    void UpdateTipVelocity(float deltaTime)
    {
        Vector3 currentTipPos = GetTipPosition();

        if (deltaTime > Mathf.Epsilon)
        {
            float tipDistace = Vector3.Distance(_previousTipPosition, currentTipPos);
            _tipVelocity = tipDistace / deltaTime;
        }
        else
        {
            _tipVelocity = 0f;
        }

        _previousTipPosition = currentTipPos;

    }



    float CalculateDamage()
    {
        // Additive damage from base damage, hit area and velocity (tip travel distance).

        float velocityDamage = _tipVelocity * _velocityDamageMultiplier;
        return _totalDamage = _damage + velocityDamage;  //_damage + CalculateVelocity();
    }

    void ApplyDamage(Collider hitCollider)
    {
        if (hitCollider == null || IsOwnedCollider(hitCollider)) return;

        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();

        if (damageable == null || _damagedTargets.Contains(damageable))
        {
            return;
        }

        _hitRegistered = true;
        _damagedTargets.Add(damageable);
        damageable.TakeDamage(CalculateDamage());
    }
    
    private bool IsOwnedCollider(Collider hitCollider)
    {
        return _ownerRoot != null && hitCollider.transform.IsChildOf(_ownerRoot);
    }

    private void OnDisable()
    {
        EndDamageWindow();
    }
}