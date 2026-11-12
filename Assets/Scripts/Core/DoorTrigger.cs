using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private string destinationName = "";

    private bool inside;
    private GameObject player;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inside = true;
        player = other.gameObject;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inside = false;
    }

    void Update()
    {
        if (!inside || player == null) return;
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;

        if (target == null)
        {
            Debug.LogWarning($"У двери {name} не задан Target");
            return;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = target.position; }
        player.transform.position = target.position;
        inside = false;
        Debug.Log($"{name} -> {target.name} ({destinationName})");
    }

    void OnDrawGizmos()
    {
        if (target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, 0.25f);
    }
}