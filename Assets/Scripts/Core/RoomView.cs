using UnityEngine;

// Комната (помещение), в которую зашёл игрок. Компонент ставится на
// триггер помещения — например, на дверь, ведущую в комнату.
// Текущая комната живёт в GameSession и дальше используется
// событиями, светом, визуалом и отчётами.
[RequireComponent(typeof(Collider2D))]
public class RoomView : MonoBehaviour
{
    // К комнате, в которую ведёт этот триггер. Заполняется в Inspector
    // или проставляется автоматически при добавлении в сцену.
    [SerializeField] private string roomId = "";

    public string RoomId => roomId;

    public bool HasRoom => !string.IsNullOrEmpty(roomId);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!HasRoom) return;

        GameSession.SetCurrentRoom(roomId);
        Debug.Log($"[RoomView] Комендант вошёл в комнату: {roomId}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
    }
}
