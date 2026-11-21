using System;
using System.Collections.Generic;

public class EventManager
{
    private readonly System.Random rng;

    public EventManager(System.Random rng)
    {
        this.rng = rng;
    }

    public bool TryApplyChoice(GameState state, GameEvent ev, int choiceIndex, Student student, Room room)
    {
        if (choiceIndex < 0 || choiceIndex >= ev.choices.Count) return false;

        var choice = ev.choices[choiceIndex];
        if (!choice.IsAvailable(state)) return false;

        var r = state.resources;
        r.Change(ResourceType.Budget, -choice.budgetCost);
        r.Change(ResourceType.Energy, -choice.energyCost);
        r.Change(ResourceType.Time, -choice.timeCost);

        // Игровое время двигается вместе с расходом часов на решение.
        GameClock.Advance(choice.timeCost);

        foreach (var effect in choice.effects)
            effect.Apply(state, student, room);

        // Отложенные последствия: случайная проверка, потом очередь
        foreach (var d in choice.delayed)
        {
            if (d.ev == null) continue;
            if (rng.NextDouble() > d.chance) continue;
            state.delayedQueue.Add(new DelayedEvent
            {
                eventId = d.ev.eventId,
                dueDay = state.day + d.daysLater,
                studentId = student?.id,
                roomId = room?.id
            });
        }

        student?.AddToHistory($"День {state.day}: {ev.title} -> {choice.text}");
        return true;
    }

    // События, срок которых наступил (утро дня)
    public List<DayEvent> TakeDueEvents(GameState state, List<GameEvent> all)
    {
        var result = new List<DayEvent>();
        var due = state.delayedQueue.FindAll(d => d.dueDay <= state.day);
        foreach (var d in due)
        {
            state.delayedQueue.Remove(d);
            var ev = all.Find(e => e.eventId == d.eventId);
            if (ev == null) continue;
            result.Add(new DayEvent
            {
                ev = ev,
                student = d.studentId == null ? null : state.GetStudent(d.studentId),
                room = d.roomId == null ? null : state.GetRoom(d.roomId)
            });
        }
        return result;
    }

    public List<DayEvent> PickDayEvents(GameState state, List<GameEvent> all, int count, System.Random rng)
    {
        var pool = all.FindAll(e => !e.delayedOnly);   // последствия случайно не выпадают
        var result = new List<DayEvent>();

        while (result.Count < count && pool.Count > 0)
        {
            var ev = PickWeighted(pool, rng);
            pool.Remove(ev);
            var dayEvent = BuildDayEvent(state, ev, rng);
            if (dayEvent != null) result.Add(dayEvent);
        }
        return result;
    }

    GameEvent PickWeighted(List<GameEvent> pool, System.Random rng)
    {
        float total = 0f;
        foreach (var e in pool) total += Math.Max(0.01f, e.baseWeight);

        double roll = rng.NextDouble() * total;
        foreach (var e in pool)
        {
            roll -= Math.Max(0.01f, e.baseWeight);
            if (roll <= 0) return e;
        }
        return pool[pool.Count - 1];
    }

    DayEvent BuildDayEvent(GameState state, GameEvent ev, System.Random rng)
    {
        var de = new DayEvent { ev = ev };

        if (ev.requiresStudent)
        {
            if (state.students.Count == 0) return null;
            de.student = state.students[rng.Next(state.students.Count)];
        }

        // 1) Жёсткая привязка к комнате из ассета (roomId) — самый точный способ.
        if (!string.IsNullOrEmpty(ev.roomId))
        {
            de.room = state.GetRoom(ev.roomId);
            if (de.room == null) return null;
        }
        else if (ev.usesRoom)
        {
            // Место выбирается строго по типу комнаты: кухня -> кухня, душ -> душ.
            var rooms = state.rooms.FindAll(r => r != null && r.type == ev.roomType);
            if (rooms.Count == 0) return null;
            de.room = rooms[rng.Next(rooms.Count)];
        }
        else if (de.student != null)
        {
            // Событие, связанное с конкретным человеком, происходит в его комнате.
            de.room = state.GetRoom(de.student.roomId);
        }
        else
        {
            // Событие без явной привязки размещаем в жилой комнате.
            var dorms = state.rooms.FindAll(r => r != null && r.type == RoomType.Dorm);
            if (dorms.Count > 0) de.room = dorms[rng.Next(dorms.Count)];
        }
        return de;
    }
}