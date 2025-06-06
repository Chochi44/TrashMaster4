using UnityEngine;

public class TruckTypeController : MonoBehaviour
{
    [Header("Truck Sprites")]
    public SpriteRenderer truckRenderer;

    [Header("Truck Type Indicators")]
    public GameObject generalIndicator;
    public GameObject paperIndicator;
    public GameObject plasticIndicator;
    public GameObject glassIndicator;

    [Header("Particle Effects")]
    public ParticleSystem collectionEffect;
    public Color generalParticleColor = Color.gray;
    public Color paperParticleColor = Color.blue;
    public Color plasticParticleColor = Color.green;
    public Color glassParticleColor = Color.white;

    private GameManager.TruckType currentType = GameManager.TruckType.General;

    private void Start()
    {
        // Initialize with general truck
        if (GameManager.Instance != null)
        {
            SetTruckType(GameManager.Instance.currentTruckType);
        }
        else
        {
            SetTruckType(GameManager.TruckType.General);
        }
    }

    public void SetTruckType(GameManager.TruckType type)
    {
        // Check if this is actually a change
        bool isTypeChange = (currentType != type);

        // Set the new type
        currentType = type;
        Debug.Log("Truck type set to: " + type);

        // Update indicators
        if (generalIndicator != null) generalIndicator.SetActive(type == GameManager.TruckType.General);
        if (paperIndicator != null) paperIndicator.SetActive(type == GameManager.TruckType.Paper);
        if (plasticIndicator != null) plasticIndicator.SetActive(type == GameManager.TruckType.Plastic);
        if (glassIndicator != null) glassIndicator.SetActive(type == GameManager.TruckType.Glass);

        // Update particle effect color
        if (collectionEffect != null)
        {
            var main = collectionEffect.main;
            switch (type)
            {
                case GameManager.TruckType.General:
                    main.startColor = generalParticleColor;
                    break;
                case GameManager.TruckType.Paper:
                    main.startColor = paperParticleColor;
                    break;
                case GameManager.TruckType.Plastic:
                    main.startColor = plasticParticleColor;
                    break;
                case GameManager.TruckType.Glass:
                    main.startColor = glassParticleColor;
                    break;
            }
        }
    }

    // Helper method to check if the truck can collect a specific trash item
    public bool CanCollectTrash(TrashItem trash)
    {
        if (trash == null) return false;

        // General truck can collect any trash
        if (currentType == GameManager.TruckType.General)
            return true;

        // Specialized trucks can only collect their type
        if (currentType == GameManager.TruckType.Paper && trash.isPaper)
            return true;

        if (currentType == GameManager.TruckType.Plastic && trash.isPlastic)
            return true;

        if (currentType == GameManager.TruckType.Glass && trash.isGlass)
            return true;

        return false;
    }

    // Play collection effect
    public void PlayCollectionEffect(Vector3 position, GameManager.TruckType trashType)
    {
        if (collectionEffect != null)
        {
            // Set color based on trash type
            var main = collectionEffect.main;
            switch (trashType)
            {
                case GameManager.TruckType.General:
                    main.startColor = generalParticleColor;
                    break;
                case GameManager.TruckType.Paper:
                    main.startColor = paperParticleColor;
                    break;
                case GameManager.TruckType.Plastic:
                    main.startColor = plasticParticleColor;
                    break;
                case GameManager.TruckType.Glass:
                    main.startColor = glassParticleColor;
                    break;
            }

            // Create effect at position
            Instantiate(collectionEffect, position, Quaternion.identity);
        }
    }

    // Update check - set truck type if it changes in GameManager
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentTruckType != currentType)
        {
            SetTruckType(GameManager.Instance.currentTruckType);
        }
    }
}