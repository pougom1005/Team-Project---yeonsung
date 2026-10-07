using UnityEngine;

public class NanoCapsule : MonoBehaviour
{
    public int healAmount = 1;

    private bool playerNearby;

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("³ª³ëÄ¸½¶ È¹µæ! È¸º¹·®: " + healAmount);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        playerNearby = true;
        Debug.Log("³ª³ëÄ¸½¶ ±ÙÃ³ÀÔ´Ï´Ù. F¸¦ ´­·¯ È¹µæÇÏ¼¼¿ä.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerNearby = false;
    }
}