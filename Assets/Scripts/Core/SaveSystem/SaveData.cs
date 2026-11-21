using System;
using System.Collections.Generic;
using UnityEngine;

// Данные, которые записываются в файл сохранения.
// Это отдельные классы, а не GameState напрямую: так формат файла
// не сломается, если позже мы добавим новые поля в игровую модель.
[Serializable]
public class SaveData
{
    // Версия формата. Если структура сохранения изменится — увеличиваем
    // и учим SaveManager переводить старые сохранения на новый формат.
    public int version = 1;

    // Когда сделано сохранение (для отладки и показа игроку).
    public string savedAtUtc = "";

    // ---- Игровые данные ----
    public int day = 1;
    public float hour = 8f;   // текущее игровое время (0..24)
    public ResourceData resources = new ResourceData();
    public List<RoomData> rooms = new List<RoomData>();
    public List<StudentData> students = new List<StudentData>();
    public List<DelayedEventData> delayed = new List<DelayedEventData>();

    // События, которые уже выпали на текущий день, но ещё не решены.
    public List<QueuedEventData> queue = new List<QueuedEventData>();

    // ---- Положение коменданта ----
    public bool hasPlayerPosition = false;
    public float playerX = 0f;
    public float playerY = 0f;
    public int floor = 2;

    // ------------------------------------------------------------------
    // GameState -> SaveData
    // ------------------------------------------------------------------
    public static SaveData FromGame(GameState state, List<DayEvent> dayQueue,
                                    Vector2 playerPosition, bool hasPlayer, int floor, float hour)
    {
        var data = new SaveData();
        if (state == null) return data;

        data.day = state.day;
        data.hour = hour;
        data.floor = floor;
        data.hasPlayerPosition = hasPlayer;
        data.playerX = playerPosition.x;
        data.playerY = playerPosition.y;

        if (state.resources != null)
        {
            data.resources = new ResourceData
            {
                budget = state.resources.budget,
                reputation = state.resources.reputation,
                energy = state.resources.energy,
                time = state.resources.time
            };
        }

        foreach (var room in state.rooms)
        {
            if (room == null) continue;
            data.rooms.Add(new RoomData
            {
                id = room.id,
                name = room.name,
                type = (int)room.type,
                floor = room.floor,
                capacity = room.capacity,
                cleanliness = room.cleanliness,
                condition = room.condition
            });
        }

        foreach (var student in state.students)
        {
            if (student == null) continue;
            var row = new StudentData
            {
                id = student.id,
                name = student.name,
                course = student.course,
                roomId = student.roomId,
                discipline = student.discipline,
                mood = student.mood,
                grades = student.grades,
                attitude = student.attitude,
                lowMoodDays = student.lowMoodDays
            };
            if (student.history != null) row.history.AddRange(student.history);
            data.students.Add(row);
        }

        foreach (var delayed in state.delayedQueue)
        {
            if (delayed == null) continue;
            data.delayed.Add(new DelayedEventData
            {
                eventId = delayed.eventId,
                dueDay = delayed.dueDay,
                studentId = delayed.studentId,
                roomId = delayed.roomId
            });
        }

        if (dayQueue != null)
        {
            foreach (var dayEvent in dayQueue)
            {
                if (dayEvent == null || dayEvent.ev == null) continue;
                data.queue.Add(new QueuedEventData
                {
                    eventId = dayEvent.ev.eventId,
                    studentId = dayEvent.student != null ? dayEvent.student.id : null,
                    roomId = dayEvent.room != null ? dayEvent.room.id : null
                });
            }
        }

        return data;
    }

    // ------------------------------------------------------------------
    // SaveData -> GameState
    // ------------------------------------------------------------------
    public GameState ToState()
    {
        var state = new GameState();
        state.day = day;

        if (resources == null) resources = new ResourceData();
        state.resources = new ResourceManager
        {
            budget = resources.budget,
            reputation = resources.reputation,
            energy = resources.energy,
            time = resources.time
        };

        if (rooms != null)
        {
            foreach (var row in rooms)
            {
                if (row == null) continue;
                state.rooms.Add(new Room(row.id, row.name, (RoomType)row.type, row.floor, row.capacity)
                {
                    cleanliness = row.cleanliness,
                    condition = row.condition
                });
            }
        }

        if (students != null)
        {
            foreach (var row in students)
            {
                if (row == null) continue;
                var student = new Student(row.id, row.name, row.course, row.roomId)
                {
                    discipline = row.discipline,
                    mood = row.mood,
                    grades = row.grades,
                    attitude = row.attitude,
                    lowMoodDays = row.lowMoodDays
                };
                if (row.history != null) student.history.AddRange(row.history);
                state.students.Add(student);
            }
        }

        if (delayed != null)
        {
            foreach (var row in delayed)
            {
                if (row == null) continue;
                state.delayedQueue.Add(new DelayedEvent
                {
                    eventId = row.eventId,
                    dueDay = row.dueDay,
                    studentId = row.studentId,
                    roomId = row.roomId
                });
            }
        }

        return state;
    }

    // ------------------------------------------------------------------
    // Восстановление очереди событий текущего дня.
    // Ссылки на ScriptableObject нельзя записать в файл, поэтому храним
    // eventId и заново находим событие в списке allEvents сцены.
    // ------------------------------------------------------------------
    public List<DayEvent> BuildQueue(List<GameEvent> allEvents, GameState state)
    {
        var result = new List<DayEvent>();
        if (queue == null || allEvents == null || state == null) return result;

        foreach (var row in queue)
        {
            if (row == null) continue;

            GameEvent ev = allEvents.Find(e => e != null && e.eventId == row.eventId);
            if (ev == null)
            {
                Debug.LogWarning($"[SaveData] Событие '{row.eventId}' не найдено в списке allEvents — пропущено.");
                continue;
            }

            result.Add(new DayEvent
            {
                ev = ev,
                student = string.IsNullOrEmpty(row.studentId) ? null : state.GetStudent(row.studentId),
                room = string.IsNullOrEmpty(row.roomId) ? null : state.GetRoom(row.roomId)
            });
        }

        return result;
    }
}

[Serializable]
public class ResourceData
{
    public float budget = 1000f;
    public int reputation = 50;
    public int energy = 100;
    public int time = 14;
}

[Serializable]
public class RoomData
{
    public string id = "";
    public string name = "";
    public int type = 0;
    public int floor = 1;
    public int capacity = 0;
    public int cleanliness = 70;
    public int condition = 80;
}

[Serializable]
public class StudentData
{
    public string id = "";
    public string name = "";
    public int course = 1;
    public string roomId = "";
    public int discipline = 50;
    public int mood = 50;
    public int grades = 50;
    public int attitude = 0;
    public int lowMoodDays = 0;
    public List<string> history = new List<string>();
}

[Serializable]
public class DelayedEventData
{
    public string eventId = "";
    public int dueDay = 1;
    public string studentId = "";
    public string roomId = "";
}

[Serializable]
public class QueuedEventData
{
    public string eventId = "";
    public string studentId = "";
    public string roomId = "";
}
