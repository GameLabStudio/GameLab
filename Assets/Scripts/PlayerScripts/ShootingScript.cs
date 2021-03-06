﻿using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(WeaponholderScript))]
public class ShootingScript : NetworkBehaviour
{
    //Public Variables
    public bool AutoReload = true;

    // Private Variables
    private InputController controls = null;
    private WeaponholderScript weaponholder = null;
    //private GunData gunData = null;


    private void Awake()
    {
        weaponholder = GetComponent<WeaponholderScript>();
        controls = new InputController();
    }


    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();



    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) return;


        WeaponData gunData = weaponholder.SelectedWeapon.GetComponent<WeaponData>();

        // if nex time to fire is reached
        if (controls.Player.Shoot.ReadValue<float>() == 1 && Time.time >= gunData.NextTimeToFire)
        {
            // calc new time to fire
            gunData.NextTimeToFire = Time.time + 1f / gunData.FireRate;

            //shoot
            TryShoot(gunData);
        }


        if (controls.Player.Reload.ReadValue<float>() == 1)
        {
            Reaload();
        }

    }


    private void TryShoot(WeaponData gunData)
    {

        if (gunData.AmmoLoaded > 0)
        {
            //Shouting Animation
            //Shouting Sound
            //MuzzleFlash

            ShootRayCast();
            CmdShoot();
            gunData.AmmoLoaded--;
        }
        else
        {
            if (AutoReload)
            {
                Reaload();
            }
            else
            {
                //Shouting Sound without bullet
            }
        }
    }


    public void Reaload()
    {

        if (!isLocalPlayer) return;

        WeaponData gunData = weaponholder.SelectedWeapon.GetComponent<WeaponData>();

        if (gunData.AmmoCount <= 0) return;

        //Relaod Animation

        int loadamount = gunData.MagSize - gunData.AmmoLoaded;

        if (gunData.AmmoCount > loadamount)
        {
            gunData.AmmoLoaded = gunData.MagSize;
            gunData.AmmoCount -= loadamount;
        }
        else
        {
            gunData.AmmoLoaded = gunData.AmmoCount;
            gunData.AmmoCount = 0;
        }
    }




    public void ShootRayCast()
    {
        //GameObject player = gameObject;
        GameObject SelectedWeapon = weaponholder.SelectedWeapon;
        WeaponData gundata = SelectedWeapon.GetComponent<WeaponData>();


        Ray ray = new Ray(gundata.WeaponMuzzle.transform.position, gundata.WeaponMuzzle.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gundata.ShootingDistance))
        {
            //spawn/despawn impact
            //GameObject bulletImpact = Instantiate(gundata.BulletImpact, position: hit.point, Quaternion.LookRotation(hit.normal));
            Quaternion quant = Quaternion.LookRotation(hit.normal);
            CmdSpawnImpact(netId, hit.point.x, hit.point.y, hit.point.z, quant.eulerAngles.x, quant.eulerAngles.y, quant.eulerAngles.z);

            //notify hitted component to be hitted (only items with networkidentity have syncroniced Health, so only they can be hitten)
            NetworkIdentity identityPlayerHit;
            if (hit.collider.TryGetComponent(out identityPlayerHit))
            {
                Cmd_Hit(this.netId, identityPlayerHit.netId, gundata.Damage);
            }

            Debug.DrawRay(gundata.WeaponMuzzle.transform.position, gundata.WeaponMuzzle.transform.forward * hit.distance, Color.blue, 2f);
        }
        else
        {
            Debug.DrawRay(gundata.WeaponMuzzle.transform.position, gundata.WeaponMuzzle.transform.forward * gundata.ShootingDistance, Color.red, 1f);
        }
    }



    [Command]
    public void CmdShoot() => RpcShoot();
    [ClientRpc]
    public void RpcShoot()
    {
        // do everithing what schoult happens on shoot
        GameObject SelectedWeapon = weaponholder.SelectedWeapon;
        WeaponData gundata = SelectedWeapon.GetComponent<WeaponData>();

        ParticleSystem flash = Instantiate(gundata.MuzzleParticles, gundata.WeaponMuzzle.transform.position, gundata.WeaponMuzzle.transform.rotation, gundata.WeaponMuzzle.transform);
        if (isLocalPlayer)
        {
            LayerMask layer = LayerMask.NameToLayer("Weapon");
            flash.gameObject.layer = layer;
            foreach (Transform t1 in flash.transform) { t1.gameObject.layer = layer; }
        }
        flash.Play();

        Destroy(flash, 0.1f);
    }




    [Command]
    private void Cmd_Hit(uint netIDFrom, uint netIDTo, float damage)
    {
        List<object> messageData = new List<object>
        {
            netIDFrom,
            damage
        };
        NetworkIdentity.spawned[netIDTo].gameObject.SendMessage("Msg_HIT", messageData, SendMessageOptions.RequireReceiver);
    }


    IEnumerator DespawnAfter1s(uint netID)
    {
        yield return new WaitForSeconds(1f);
        if (!isServer) CmdDespawn(netID);
        else
        {
            GameObject gm = NetworkIdentity.spawned[netID].gameObject;
            NetworkServer.Destroy(gm);
        }
    }


    [Command]
    private void CmdSpawnImpact(uint netID, float x, float y, float z, float eulerx, float eulery, float eulerz)
    {
        //GameObject player = NetworkIdentity.spawned[netID].gameObject;
        GameObject SelectedWeapon = weaponholder.SelectedWeapon;
        WeaponData gundata = SelectedWeapon.GetComponent<WeaponData>();

        GameObject impact = Instantiate(gundata.BulletImpact, new Vector3(x, y, z), Quaternion.Euler(eulerx, eulery, eulerz));

        NetworkServer.Spawn(impact);

        StartCoroutine("DespawnAfter1s", impact.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    private void CmdDespawn(uint netID)
    {
        GameObject gm = NetworkIdentity.spawned[netID].gameObject;
        NetworkServer.Destroy(gm);
    }






}




