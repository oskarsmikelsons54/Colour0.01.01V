using System.Collections;
using UnityEngine;

public class LightBulb : MonoBehaviour
{
    public float speed = 7f;
    public float chargeTime = 1f;

    public float damage = 0f;

    private Transform target;
    private Vector3 moveDirection;
    private bool isReadyToFire = false;

    public void SetTarget(Transform playerTransform)
    {
        target = playerTransform;
        StartCoroutine(ChargeAndFire());
    }

    IEnumerator ChargeAndFire()
    {
        yield return new WaitForSeconds(chargeTime);

        if (target != null)
        {
            moveDirection = (target.position - transform.position).normalized;
            isReadyToFire = true;

            Destroy(gameObject, 5f);
        }
    }

    void Update()
    {
        if (isReadyToFire)
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Spuldze trāpīja spēlētājam!");

            // 🔥 FIX: meklē PlayerHealth arī parentā
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log("Damage applied: " + damage);
            }
            else
            {
                Debug.Log("PlayerHealth NOT FOUND!");
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}