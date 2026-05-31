using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class Combat : MonoBehaviour
{
    // Targeting
    // Weapon IK

    public Transform WeaponMaster;
    public Transform Chest;


    private void Start()
    {
        WeaponMaster.SetParent(Chest, false);
    }
}

public class Weapon : MonoBehaviour
{
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

    

    private void OnTriggerEnter(Collider other)
    {
        _hitRegistered = true;
    }

    //NOTE: pseudo-code.
    float CalculateVelocity()
    {
        // Current tip location -> final tip location on hit.
        
        //TEMP
        float velocity = 0;
        
        float time = 0;
        Vector3 startPos = _swordTip.transform.position;
        Vector3 finalPos = Vector3.zero;
        // is this checking time it takes from current to final?


        if (_attackStarted)
        {
            time = Time.deltaTime;

        }
        //This could probably be an OnTrigger event 
        

        if (_hitRegistered)
        {
            finalPos = _swordTip.transform.position;
            float d = Vector3.Distance(startPos, finalPos);

            velocity += time / d;
        }


        _hitRegistered = false;
        time = 0;

        return velocity;
    }

    void CalculateDamage()
    {
        // Additive damage from base damage, hit area and velocity (tip travel distance).

        _totalDamage += _damage + CalculateVelocity();
    }

    void ApplyDamage()
    {
        
    }
}

public interface IDamageable
{
    void TakeDamage(float damage);
}