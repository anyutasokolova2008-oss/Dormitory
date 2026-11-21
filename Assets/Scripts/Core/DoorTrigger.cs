using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private string destinationName = "";

    // Все двери, в зоне которых сейчас находится игрок.
    // Нужны для единого нажатия E: если игрок у двери, E означает переход.
    private static readonly List<DoorTrigger> doorsInRange = new List<DoorTrigger>();

    private bool inside;
    private GameObject player;

    // При входе в Play mode статический список нужно очистить,
    // иначе в нём могут остаться двери прошлого запуска.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        doorsInRange.Clear();
    }

    // Текст подсказки, если игрок стоит у работающей двери.
    // null — подсказки для E нет.
    public static string GetPromptText()
    {
        return GetUsableDoor() != null ? "E — перейти" : null;
    }

    // Есть ли рядом дверь, у которой задан Target (т.е. E сработает).
    public static bool HasUsableDoorInRange => GetUsableDoor() != null;

    // Игрок находится в зоне хотя бы одной двери.
    public static bool HasDoorInRange
    {
        get
        {
            for (int i = doorsInRange.Count - 1; i >= 0; i--)
            {
                var door = doorsInRange[i];
                if (door == null || !door.isActiveAndEnabled)
                {
                    doorsInRange.RemoveAt(i);
                    continue;
                }
                return true;
            }
            return false;
        }
    }

    // Выполняет переход через ближайшую дверь.
    // Возвращает false, если игрок не у двери или у двери не задан Target.
    public static bool TryUseNearestDoor()
    {
        var door = GetUsableDoor();
        if (door == null) return false;
        return door.UseDoor();
    }

    // Настройка двери из кода (для служебных дверей 1 этажа,
    // которые создаются в GameBootstrap).
    public void Configure(Transform newTarget, string newDestinationName)
    {
        target = newTarget;
        destinationName = newDestinationName ?? "";
    }

    // Последняя дверь, в которую вошёл игрок (на случай пересекающихся зон).
    private static DoorTrigger GetUsableDoor()
    {
        for (int i = doorsInRange.Count - 1; i >= 0; i--)
        {
            var door = doorsInRange[i];
            if (door == null)
            {
                doorsInRange.RemoveAt(i);
                continue;
            }

            if (!door.isActiveAndEnabled || door.target == null) continue;
            return door;
        }

        return null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (inside) return;
        inside = true;
        player = other.gameObject;

        if (!doorsInRange.Contains(this)) doorsInRange.Add(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!inside) return;

        inside = false;
        player = null;
        doorsInRange.Remove(this);
    }

    // Если дверь выключили/удалили, пока игрок стоял в её зоне,
    // убираем её из списка, чтобы зона двери не «залипла».
    void OnDisable()
    {
        if (!inside) return;
        inside = false;
        player = null;
        doorsInRange.Remove(this);
    }

    // Сам переход. Вызывается из GameBootstrap по нажатию E.
    private bool UseDoor()
    {
        if (target == null)
        {
            Debug.LogWarning($"У двери {name} не задан Target");
            return false;
        }

        if (player == null)
        {
            Debug.LogWarning($"У двери {name} нет игрока — переход отменён");
            return false;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Без снятия интерполяции игrok «вылетает» из точки телепорта.
            var oldInterpolation = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.linearVelocity = Vector2.zero;
            rb.position = target.position;
            Physics2D.SyncTransforms();
            rb.position = target.position;
            rb.interpolation = oldInterpolation;
        }
        player.transform.position = target.position;

        // После телепорта игрок покидает зону этой двери.
        inside = false;
        player = null;
        doorsInRange.Remove(this);

        // Комната после телепорта — это комната назначения.
        // Иначе игрок «оставался» в той комнате, из которой вышел.
        var arrivedRoomId = ResolveRoomId(destinationName, target.position);
        if (arrivedRoomId != null) GameSession.SetCurrentRoom(arrivedRoomId);

        Debug.Log($"{name} -> {target.name} ({destinationName}), room={arrivedRoomId ?? "?"}");
        return true;
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

        // Переходы между этажами: "Первый Этаж" / "Второй Этаж".
        if (ContainsIgnoreCase(dest, "первый") && ContainsIgnoreCase(dest, "этаж"))
            return "corridor1";
        if (ContainsIgnoreCase(dest, "второй") && ContainsIgnoreCase(dest, "этаж"))
            return "corridor2";

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
