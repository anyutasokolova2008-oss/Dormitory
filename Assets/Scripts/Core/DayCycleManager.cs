using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class DayCycleManager
{
    public const int LastDay = 15;      // длительность прототипа (позже 30-60)
    public const int EventsPerDay = 4;

    private readonly GameState state;
    private readonly EventManager events;
    private readonly List<GameEvent> allEvents;
    private readonly System.Random rng;
    private readonly List<DayEvent> today = new List<DayEvent>();

    public bool IsOver { get; private set; }
    public bool Defeated { get; private set; }

    public DayCycleManager(GameState state, EventManager events, List<GameEvent> allEvents, System.Random rng)
    {
        this.state = state;
        this.events = events;
        this.allEvents = allEvents;
        this.rng = rng;
    }

    // Есть ли ещё неразрешённые события сегодняшнего дня.
    public bool HasPendingEvents => today.Count > 0;

    // Совместимость со старым кодом.
    public bool HasNextEvent => HasPendingEvents;
    public DayEvent NextEvent() => today.Count > 0 ? today[0] : null;

    // Снимок оставшихся событий дня (нужен для сохранения игры).
    public List<DayEvent> SnapshotToday()
    {
        return new List<DayEvent>(today);
    }

    // Восстанавливает оставшиеся события из сохранения.
    // НЕ вызывает StartDay(): ресурсы и день уже восстановлены из сейва.
    public void RestoreToday(IEnumerable<DayEvent> events)
    {
        today.Clear();
        if (events == null) return;

        foreach (var e in events)
        {
            if (e == null || e.ev == null) continue;
            today.Add(e);
        }
    }

    // Ищет событие, привязанное к комнате.
    // 1) Точное совпадение комнаты (или событие без места — доступно везде).
    // 2) Общие зоны одного типа (душ/кухня/туалет/коридор): любая комната того же типа подходит.
    // Жилые события открываются строго в своей комнате.
    public DayEvent GetPendingEventForRoom(string roomId)
    {
        // 1) Точное совпадение / событие без места
        for (int i = 0; i < today.Count; i++)
        {
            var e = today[i];
            if (e == null) continue;
            if (e.room == null || string.IsNullOrEmpty(e.room.id)) return e;
            if (!string.IsNullOrEmpty(roomId) && e.room.id == roomId) return e;
        }

        // Событие открывается строго в своей комнате.
        // Раньше здесь был фолбэк по типу помещения: из-за него событие
        // кухни/душа открывалось в любой комнате того же типа («как попало»).
        return null;
    }

    // Определяет тип/этаж текущей комнаты. Работает даже если id (например r208)
    // отсутствует в реестре state.rooms.
    (RoomType type, int floor) ResolveRoomInfo(string roomId)
    {
        var room = state != null ? state.GetRoom(roomId) : null;
        if (room != null) return (room.type, room.floor);

        if (roomId.StartsWith("corridor")) return (RoomType.Corridor, FloorFromRoomId(roomId, 1));
        if (roomId.StartsWith("shower")) return (RoomType.Shower, FloorFromRoomId(roomId, 2));
        if (roomId.StartsWith("kitchen")) return (RoomType.Kitchen, FloorFromRoomId(roomId, 2));
        if (roomId.StartsWith("toilet")) return (RoomType.Toilet, FloorFromRoomId(roomId, 2));
        if (roomId.StartsWith("guardpost")) return (RoomType.Guardpost, 1);
        if (roomId.StartsWith("office") || roomId.StartsWith("med")) return (RoomType.Office, 1);
        if (roomId.StartsWith("storage")) return (RoomType.Storage, 1);

        if (roomId.StartsWith("r") && roomId.Length > 1)
        {
            var digits = new string(roomId.Substring(1).TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var number))
            {
                int floor = number >= 200 ? 2 : 1;
                return (RoomType.Dorm, floor);
            }
        }

        return (RoomType.Corridor, 1);
    }

    static int FloorFromRoomId(string roomId, int fallback)
    {
        var digits = new string(roomId.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var n)) return n;
        return fallback;
    }

    // Убирает событие из списка сегодняшних (после обработки игроком).
    public bool RemovePendingEvent(DayEvent ev)
    {
        if (ev == null) return false;
        return today.Remove(ev);
    }

    public void StartDay()
    {
        GameClock.ResetToDayStart();

        var r = state.resources;
        r.Change(ResourceType.Energy, 100);
        r.Change(ResourceType.Time, 14);

        today.Clear();

        // Сначала отложенные события (должны сработать сегодня), потом обычные
        var due = events.TakeDueEvents(state, allEvents);
        foreach (var e in due) today.Add(EnsureRoom(e));

        int rest = Mathf.Max(0, EventsPerDay - due.Count);
        foreach (var e in events.PickDayEvents(state, allEvents, rest, rng))
            today.Add(EnsureRoom(e));
    }

    // Гарантирует, что у события есть место. Если места нет — привязываем
    // к жилой комнате (чтобы бытовые события не «висели» в коридоре), иначе к коридору.
    DayEvent EnsureRoom(DayEvent e)
    {
        if (e == null) return e;
        if (e.room != null && !string.IsNullOrEmpty(e.room.id)) return e;

        Room room = null;
        if (state != null && state.rooms != null)
            room = state.rooms.Find(r => r != null && r.type == RoomType.Dorm)
                ?? state.rooms.Find(r => r != null && r.type == RoomType.Corridor)
                ?? (state.rooms.Count > 0 ? state.rooms[0] : null);

        e.room = room;
        return e;
    }

    // Конец дня. Возвращает текст отчёта.
    public string EndDay()
    {
        var sb = new StringBuilder();
        var r = state.resources;

        // Финансы: выплата раз в неделю + износ помещений
        if (state.day % 7 == 0)
        {
            r.Change(ResourceType.Budget, 150);
            sb.AppendLine("Выплата финансирования: +150 у.е.");
        }
        foreach (var room in state.rooms)
            room.Worsen(3, 1);

        // Люди: расчёт настроения и критических состояний
        int critical = 0;
        foreach (var s in state.students)
        {
            var room = state.GetRoom(s.roomId);
            float clean = room != null ? room.cleanliness : 50f;
            float cond = room != null ? room.condition : 50f;
            float dm = 4f * (clean - 50f) / 50f + 3f * (cond - 50f) / 50f;

            s.ApplyEffect(StudentStat.Mood, Mathf.RoundToInt(dm));
            s.lowMoodDays = s.mood < 10 ? s.lowMoodDays + 1 : 0;

            if (s.GetState() == StudentState.Critical) critical++;
        }
        if (critical > 0)
        {
            r.Change(ResourceType.Reputation, -critical);
            sb.AppendLine($"Студенты в критике: {critical} (репутация -{critical})");
        }

        sb.AppendLine($"Бюджет: {r.budget:0} у.е.");
        sb.AppendLine($"Репутация: {r.reputation}/100");
        sb.AppendLine($"Среднее настроение: {state.students.Average(s => s.mood):0}");

        // Переход к следующему дню
        state.day++;
        if (r.IsDefeated())
        {
            IsOver = true;
            Defeated = true;
        }
        else if (state.day > LastDay)
        {
            IsOver = true;
        }
        return sb.ToString();
    }

    // Рейтинг (итог игры)
    public float CalculateRating()
    {
        var res = state.resources;
        float b = Mathf.Clamp(res.budget / 1000f * 50f, 0f, 100f);
        float m = (float)state.students.Average(s => s.mood);
        float d = (float)state.students.Average(s => s.discipline);
        float sanitary = state.rooms.Average(r => (r.cleanliness + r.condition) / 2f);

        return 0.35f * res.reputation + 0.25f * b + 0.20f * m + 0.10f * d + 0.10f * sanitary;
    }

    public static string GetEnding(float rating)
    {
        if (rating >= 80f) return "Общежитие процветает";
        if (rating >= 60f) return "Стабильная работа";
        if (rating >= 40f) return "Еле удержались";
        return "Провал";
    }
}
