using System.Linq;
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
        if (inside) return;
        inside = true;
        player = other.gameObject;
        GameBootstrap.SetPlayerInDoorZone(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!inside) return;
        inside = false;
        GameBootstrap.SetPlayerInDoorZone(false);
    }

    void Update()
    {
        if (!inside || player == null) return;
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
        if (GameBootstrap.IsEventWindowOpen()) return;

        if (target == null)
        {
            Debug.LogWarning($"У двери {name} не задан Target");
            return;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = target.position; }
        player.transform.position = target.position;

        // После телепорта игрок покидает зону этой двери.
        inside = false;
        GameBootstrap.SetPlayerInDoorZone(false);

        // Комната после телепорта — это комната назначения.
        // Иначе игрок «оставался» в той комнате, из которой вышел.
        var arrivedRoomId = ResolveRoomId(destinationName, target.position);
        if (arrivedRoomId != null) GameSession.SetCurrentRoom(arrivedRoomId);

        // Это же нажатие E использовано для перехода — не открывать событие в этот кадр.
        GameBootstrap.SuppressRoomEventInteractionThisFrame();

        Debug.Log($"{name} -> {target.name} ({destinationName}), room={arrivedRoomId ?? "?"}");
    }

    // Ищет комнату по имени/идентификатору назначения.
    // Примеры: "Комната 101" -> r101, "Коридор" -> corridor1/corridor2 по этажу.
    string ResolveRoomId(string destinationName, Vector2 targetPosition)
    {
        if (GameSession.Current == null || string.IsNullOrEmpty(destinationName)) return null;

        var dest = destinationName.Trim().Trim('"');
        var rooms = GameSession.Current.rooms;
        if (rooms == null) return null;

        var room = rooms.Find(r => r != null && string.Equals(r.name, dest, System.StringComparison.OrdinalIgnoreCase))
                 ?? rooms.Find(r => r != null && string.Equals(r.id, dest, System.StringComparison.OrdinalIgnoreCase));

        if (room != null) return room.id;

        var digits = new string(dest.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out var number))
        {
            var byId = rooms.Find(r => r != null && r.id == $"r{number}");
            return byId != null ? byId.id : $"r{number}";
        }

        int floor = GameSession.FloorFromPosition(targetPosition);
        if (ContainsIgnoreCase(dest, "коридор"))
            return rooms.Find(r => r != null && r.type == RoomType.Corridor && r.floor == floor)?.id
                ?? (floor <= 1 ? "corridor1" : "corridor2");
        if (ContainsIgnoreCase(dest, "душ") || ContainsIgnoreCase(dest, "bath"))
            return rooms.Find(r => r != null && r.type == RoomType.Shower && r.floor == floor)?.id
                ?? rooms.Find(r => r != null && r.type == RoomType.Shower)?.id;
        if (ContainsIgnoreCase(dest, "кух"))
            return rooms.Find(r => r != null && r.type == RoomType.Kitchen && r.floor == floor)?.id
                ?? rooms.Find(r => r != null && r.type == RoomType.Kitchen)?.id;
        if (ContainsIgnoreCase(dest, "туал"))
            return rooms.Find(r => r != null && r.type == RoomType.Toilet && r.floor == floor)?.id
                ?? rooms.Find(r => r != null && r.type == RoomType.Toilet)?.id;
        if (ContainsIgnoreCase(dest, "вахт"))
            return rooms.Find(r => r != null && r.type == RoomType.Guardpost)?.id ?? "guardpost1";
        if (ContainsIgnoreCase(dest, "воспит"))
            return rooms.Find(r => r != null && r.id == "office1")?.id ?? "office1";
        if (ContainsIgnoreCase(dest, "медпункт") || ContainsIgnoreCase(dest, "мед"))
            return rooms.Find(r => r != null && r.id == "med1")?.id ?? "med1";
        if (ContainsIgnoreCase(dest, "склад") || ContainsIgnoreCase(dest, "кладов"))
            return rooms.Find(r => r != null && r.type == RoomType.Storage)?.id ?? "storage1";

        return null;
    }

    static bool ContainsIgnoreCase(string value, string fragment)
    {
        return value != null && value.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void OnDrawGizmos()
    {
        if (target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, 0.25f);
    }
}
