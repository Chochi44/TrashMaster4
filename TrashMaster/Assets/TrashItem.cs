using UnityEngine;

public class TrashItem : MonoBehaviour
{
    [Header("Trash Type")]
    public bool isPaper = false;
    public bool isPlastic = false;
    public bool isGlass = false;
    public bool isGeneral = false;

    [Header("Game Properties")]
    public int pointValue = 100;
    public bool isCollected = false;

    // Visual effect for collection
    public GameObject collectionEffectPrefab;

    public void Collect()
    {
        if (!isCollected)
        {
            isCollected = true;

            // Play collection effect if available
            if (collectionEffectPrefab != null)
            {
                Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Hide the trash item
            gameObject.SetActive(false);
        }
    }

    // This is called by the LevelManager when the trash goes off-screen without being collected
    public void Missed()
    {
        if (!isCollected)
        {
            // Just deactivate the object
            gameObject.SetActive(false);
        }
    }
}