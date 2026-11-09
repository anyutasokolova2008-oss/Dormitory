using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class DayCycleManager
{
    public const int LastDay = 15;      // длина демо-сессии (потом 30-60)
    public const int EventsPerDay = 4;

    private readonly GameState state;
    private readonly EventManager events;
    private readonly List<GameEvent> allEvents;
    private readonly System.Random rng;
    private readonly Queue<DayEvent> today = new Queue<DayEvent>();

    public bool IsOver { get; private set; }
    public bool Defeated { get; private set; }

    public DayCycleManager(GameState state, EventManager events, List<GameEvent> allEvents, System.Random rng)
    {
        this.state = state;
        this.events = events;
        this.allEvents = allEvents;
        this.rng = rng;
    }

    public bool HasNextEvent => today.Count > 0;
    public DayEvent NextEvent() => today.Dequeue();

    // Утро
    public void StartDay()
    {
        var r = state.resources;
        r.Change(ResourceType.Energy, 100);
        r.Change(ResourceType.Time, 14);

        today.Clear();
        foreach (var e in events.PickDayEvents(state, allEvents, EventsPerDay, rng))
            today.Enqueue(e);
    }

    // Вечер и ночь. Возвращает текст отчёта дня
    public string EndDay()
    {
        var sb = new StringBuilder();
        var r = state.resources;

        // Вечер: дотация раз в неделю и износ помещений
        if (state.day % 7 == 0)
        {
            r.Change(ResourceType.Budget, 700);
            sb.AppendLine("Дотация администрации: +700 руб.");
        }
        foreach (var room in state.rooms)
            room.Worsen(3, 1);

        // Ночь: формула (2.3), конфликты пока не учитываем
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
            sb.AppendLine($"Жильцов на пределе: {critical} (репутация -{critical})");
        }

        sb.AppendLine($"Бюджет: {r.budget:0} руб.");
        sb.AppendLine($"Репутация: {r.reputation}/100");
        sb.AppendLine($"Среднее настроение жильцов: {state.students.Average(s => s.mood):0}");

        // Проверка завершения
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

    // Формула (2.4)
    public float CalculateRating()
    {
        var res = state.resources;
        float b = Mathf.Clamp(res.budget / 5000f * 50f, 0f, 100f);
        float m = (float)state.students.Average(s => s.mood);
        float d = (float)state.students.Average(s => s.discipline);
        float sanitary = state.rooms.Average(r => (r.cleanliness + r.condition) / 2f);

        return 0.35f * res.reputation + 0.25f * b + 0.20f * m + 0.10f * d + 0.10f * sanitary;
    }

    public static string GetEnding(float rating)
    {
        if (rating >= 80f) return "Образцовое общежитие";
        if (rating >= 60f) return "Стабильная работа";
        if (rating >= 40f) return "Едва удержался";
        return "Увольнение";
    }
}