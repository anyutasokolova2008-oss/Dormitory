using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public int day = 1;
    public ResourceManager resources = new ResourceManager();
    public List<Student> students = new List<Student>();
    public List<Room> rooms = new List<Room>();
    public List<DelayedEvent> delayedQueue = new List<DelayedEvent>();

    // Реестр помещений жилого дома по фактической планировке сцены PlayTest:
    // 2 этаж — жилые комнаты, кухня, душевая, туалет, коридор;
    // 1 этаж — вахта, воспитательская, медпункт, склад, коридор.
    // roomId (id) совпадает с roomId у жильцов и с id/местом событий.
    public static GameState CreateNew()
    {
        var s = new GameState();

        s.rooms.Add(new Room("corridor1", "Коридор, 1 этаж", RoomType.Corridor, 1, 0));
        s.rooms.Add(new Room("corridor2", "Коридор, 2 этаж", RoomType.Corridor, 2, 0));

        // Общие зоны 2 этажа
        s.rooms.Add(new Room("shower2", "Душевая, 2 этаж", RoomType.Shower, 2, 0));
        s.rooms.Add(new Room("kitchen2", "Кухня, 2 этаж", RoomType.Kitchen, 2, 0));
        s.rooms.Add(new Room("toilet2", "Туалет, 2 этаж", RoomType.Toilet, 2, 0));

        // Служебные помещения 1 этажа
        s.rooms.Add(new Room("guardpost1", "Вахта", RoomType.Guardpost, 1, 0));
        s.rooms.Add(new Room("office1", "Воспитательская", RoomType.Office, 1, 0));
        s.rooms.Add(new Room("med1", "Медпункт", RoomType.Office, 1, 0));
        s.rooms.Add(new Room("storage1", "Склад", RoomType.Storage, 1, 0));

        // Жилые комнаты, присутствующие в сцене PlayTest (2 этаж).
        s.rooms.Add(new Room("r201", "Комната 201", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r203", "Комната 203", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r205", "Комната 205", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r206", "Комната 206", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r207", "Комната 207", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r208", "Комната 208", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r209", "Комната 209", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r210", "Комната 210", RoomType.Dorm, 2, 2));
        s.rooms.Add(new Room("r211", "Комната 211", RoomType.Dorm, 2, 2));

        // Жильцы живут в реальных комнатах сцены — события, связанные с человеком,
        // будут появляться в его комнате.
        s.students.Add(new Student("s1", "Артём", 2, "r201"));
        s.students.Add(new Student("s2", "Максим", 2, "r201"));
        s.students.Add(new Student("s3", "Аня", 1, "r203"));
        s.students.Add(new Student("s4", "Валерия", 1, "r203"));
        s.students.Add(new Student("s5", "Иван", 3, "r205"));

        return s;
    }

    public Student GetStudent(string id) => students != null ? students.Find(x => x != null && x.id == id) : null;
    public Room GetRoom(string id) => rooms != null ? rooms.Find(x => x != null && x.id == id) : null;
}