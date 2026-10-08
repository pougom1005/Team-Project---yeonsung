using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    protected int HP;
    protected int PatternDelay;
    protected int AttackDamage;
    protected void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 근처에 접근");
        }
    }

    protected void Start()
    {
    }

    protected void Update()
    {
        if (HP <= 0)
        {
            Death();
            Destroy(gameObject);
        }
    }
    protected void Death()
    {
        gameObject.SetActive(false);
    }
}