using UnityEngine;

public class KitchenZoneTest : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"KitchenInteractionZone: обнаружен объект {other.name}, Tag: {other.tag}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"KitchenInteractionZone: объект {other.name} покинул зону");
    }
}