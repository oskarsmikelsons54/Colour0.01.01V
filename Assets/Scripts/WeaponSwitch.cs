using UnityEngine;

// Handles switching between weapon components on the player.
// Add this to the player GameObject and assign the components via the Inspector.
public class WeaponSwitch : MonoBehaviour
{
    [Tooltip("Component that implements the melee weapon (e.g. PlayerMeleeAttack)")]
    [SerializeField] private MonoBehaviour meleeComponent;

    [Tooltip("Component that implements the ranged weapon (e.g. PlayerRangedAttack)")]
    [SerializeField] private MonoBehaviour rangedComponent;

    [Header("Input")]
    [Tooltip("Key used to toggle between weapons")]
    [SerializeField] private KeyCode switchKey = KeyCode.C;

    [Header("Initial State")]
    [Tooltip("If true, start with ranged weapon active; otherwise start with melee active")]
    [SerializeField] private bool startRanged = false;

    void Start()
    {
        // ensure initial state
        if (meleeComponent != null) meleeComponent.enabled = !startRanged;
        if (rangedComponent != null) rangedComponent.enabled = startRanged;
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ToggleWeapon();
        }
    }

    public void ToggleWeapon()
    {
        if (meleeComponent == null || rangedComponent == null) return;

        bool nowRanged = !rangedComponent.enabled;
        rangedComponent.enabled = nowRanged;
        meleeComponent.enabled = !nowRanged;

        Debug.Log(nowRanged ? "Switched to ranged" : "Switched to melee");
    }
}
