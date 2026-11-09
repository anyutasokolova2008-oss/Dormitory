using System;
using System.Collections.Generic;

public class EventManager
{
    // Последовательность из раздела 2.3: проверка, списание затрат, эффекты, история
    public bool TryApplyChoice(GameState state, GameEvent ev, int choiceIndex, Student student, Room room)
    {
        if (choiceIndex < 0 || choiceIndex >= ev.choices.Count) return false;

        var choice = ev.choices[choiceIndex];
        if (!choice.IsAvailable(state)) return false;

        var r = state.resources;
        r.Change(ResourceType.Budget, -choice.budgetCost);
        r.Change(ResourceType.Energy, -choice.energyCost);
        r.Change(ResourceType.Time, -choice.timeCost);

        foreach (var effect in choice.effects)
            effect.Apply(state, student, room);

        student?.AddToHistory($"День {state.day}: {ev.title} -> {choice.text}");
        return true;
    }

    // Выбирает события на день без повторов внутри одного дня
    public List<DayEvent> PickDayEvents(GameState state, List<GameEvent> all, int count, System.Random rng)
    {
        var pool = new List<GameEvent>(all);
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

    // Подставляет случайного студента и подходящее помещение
    DayEvent BuildDayEvent(GameState state, GameEvent ev, System.Random rng)
    {
        var de = new DayEvent { ev = ev };

        if (ev.requiresStudent)
        {
            if (state.students.Count == 0) return null;
            de.student = state.students[rng.Next(state.students.Count)];
        }

        if (ev.usesRoom)
        {
            var rooms = state.rooms.FindAll(r => r.type == ev.roomType);
            if (rooms.Count == 0) return null;
            de.room = rooms[rng.Next(rooms.Count)];
        }
        else if (de.student != null)
        {
            de.room = state.GetRoom(de.student.roomId); // событие в комнате самого студента
        }
        return de;
    }
}