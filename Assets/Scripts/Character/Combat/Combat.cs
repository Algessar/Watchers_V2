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


public interface IDamageable
{
    void TakeDamage(float damage);
}