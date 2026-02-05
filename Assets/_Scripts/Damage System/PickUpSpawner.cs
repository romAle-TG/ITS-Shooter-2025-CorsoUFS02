using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]

public enum PickUpSpawnType
{
    None = 0,
    HEAL = 3,
    WEAPON = 2,
    AMMO = 3
}

public class PickUpSpawner : MonoBehaviour
{
    [SerializeField] PickUp healPickUp;
    [SerializeField] PickUp weaponPickUp;

    [SerializeField] PickUpSpawnType spawnType;
    [SerializeField] List<WeaponData> elegibleWeapon = new();

    [SerializeField] bool startWithPickUp;
    [SerializeField] float cooldown;


}
