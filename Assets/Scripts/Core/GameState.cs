using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public int day = 1;
    public ResourceManager resources = new ResourceManager();
    public List<Student> students = new List<Student>();
    public List<Room> rooms = new List<Room>();

    // Тестовые данные: три помещения и пять проживающих (этап 3 плана)
    public static GameState CreateNew()
    {
        var s = new GameState();

        s.rooms.Add(new Room("r101", "Комната 101", RoomType.Dorm, 1, 2));
        s.rooms.Add(new Room("r102", "Комната 102", RoomType.Dorm, 1, 2));
        s.rooms.Add(new Room("shower1", "Душевая 1 этажа", RoomType.Shower, 1, 0));
        s.rooms.Add(new Room("kitchen1", "Кухня 1 этажа", RoomType.Kitchen, 1, 0));

        s.students.Add(new Student("s1", "Артём", 2, "r101"));
        s.students.Add(new Student("s2", "Максим", 2, "r101"));
        s.students.Add(new Student("s3", "Аня", 1, "r102"));
        s.students.Add(new Student("s4", "Валерия", 1, "r102"));
        s.students.Add(new Student("s5", "Иван", 3, "r102"));

        return s;
    }

    public Student GetStudent(string id) => students.Find(x => x.id == id);
    public Room GetRoom(string id) => rooms.Find(x => x.id == id);
}