using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public GameObject weapon1;
    public GameObject weapon2;

    void Start()
    {
        // Start with weapon1 active
        weapon1.SetActive(true);
        weapon2.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipWeapon1();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipWeapon2();
        }
    }

    void EquipWeapon1()
    {
        weapon1.SetActive(true);
        weapon2.SetActive(false);
    }

    void EquipWeapon2()
    {
        weapon1.SetActive(false);
        weapon2.SetActive(true);
    }
}