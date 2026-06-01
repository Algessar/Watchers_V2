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
    private float _totalDamage;
    private float _tipVelocity;

    private bool _attackStarted;
    private bool _hitRegistered;

    private readonly HashSet<IDamageable> _damagedTargets = new();

    private void Awake()
    {
        if (_ownerRoot == null)
        {
            _ownerRoot = transform.root; //NOTE: No idea what this is for really
        }
    }

    private void OnEnable()
    {
        ResetHitRegistration();
    }

    private void ResetHitRegistration()
    {
        _hitRegistered = false;
        _totalDamage = 0;
        _damagedTargets.Clear();

    }

    private void OnTriggerEnter(Collider other)
    {
       // _hitRegistered = true;
       ApplyDamage(other);
    }

    public void BeginDamageWindow()
    {
        _attackStarted = true;
        ResetHitRegistration();

        if (_tipCollider != null)
        {
            _tipCollider.enabled = true;
        }
    }

    public void EndDamageWindow()
    {
        _attackStarted = false;
        if (_tipCollider != null)
        {
            _tipCollider.enabled = false;
        }
    }
    
    

    //NOTE: pseudo-code.
    float CalculateVelocity()
    {
        // Current tip location -> final tip location on hit.
        
        //TEMP
        float velocity = 0;
        
        float time = 0;
        Vector3 startPos = _swordTip != null ? _swordTip.transform.position : transform.position;
        Vector3 finalPos = Vector3.zero;
        // is this checking time it takes from current to final?


        if (_attackStarted)
        {
            time = Time.deltaTime;

        }
        //This could probably be an OnTrigger event 
        

        if (_hitRegistered)
        {
            
            finalPos = _swordTip != null ? _swordTip.transform.position : transform.position;
            float d = Vector3.Distance(startPos, finalPos);

            if (d > Mathf.Epsilon)
            {
                velocity += d / time;
            }
        }


        _hitRegistered = false;
        time = 0;

        return velocity;
    }

    float CalculateDamage()
    {
        // Additive damage from base damage, hit area and velocity (tip travel distance).

        return _totalDamage = _damage + CalculateVelocity();
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
}