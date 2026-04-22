using System.Collections;
using UnityEngine;

public class MiniBoss : MonoBehaviour
{
    public Transform player;
    public GameObject lightBulbPrefab;
    
    [Header("Saites ar esošo sistēmu")]
    private EnemyHealth healthScript; // Tavs esošais skripts

    [Header("Fāze 1 Iestatījumi")]
    public float spawnRadius = 2.5f;
    public float attackCooldown = 3f;
    public int bulbsPerAttack = 6;

    [Header("Fāze 2 Iestatījumi")]
    public GameObject[] fakeAdsUI;
    private bool isPhase2 = false;

    void Start()
    {
        // Atrodam EnemyHealth komponenti, kas uzlikta uz šī paša objekta
        healthScript = GetComponent<EnemyHealth>();
        StartCoroutine(BossAttackPattern());
    }

    void Update()
    {
        // Pārbaudām fāzi katrā kadrā, skatoties tavu EnemyHealth skriptu
        if (healthScript != null && !isPhase2)
        {
            // Pārejam uz 2. fāzi, ja HP ir zem 40%
            if (healthScript.currentHealth <= healthScript.maxHealth * 0.4f)
            {
                EnterPhase2();
            }
        }
    }

    IEnumerator BossAttackPattern()
    {
        while (true)
        {
            for (int i = 0; i < bulbsPerAttack; i++)
            {
                float angle = i * Mathf.PI * 2 / bulbsPerAttack;
                Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnRadius;
                
                GameObject bulb = Instantiate(lightBulbPrefab, spawnPos, Quaternion.identity);
                bulb.GetComponent<LightBulb>().SetTarget(player);
            }

            float cooldown = isPhase2 ? attackCooldown / 2f : attackCooldown;
            yield return new WaitForSeconds(cooldown);
        }
    }

    void EnterPhase2()
    {
        isPhase2 = true;
        bulbsPerAttack += 2;
        foreach (GameObject ad in fakeAdsUI)
        {
            if (ad != null) ad.SetActive(true);
        }
        Debug.Log("2. FĀZE SĀKUSIES!");
    }
}