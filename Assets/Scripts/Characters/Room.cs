using System;
using UnityEngine;

public enum RoomType { Dorm, Kitchen, Shower, Toilet, Corridor, Guardpost, Office, Storage, Yard }

[Serializable]
public class Room
{
    public string id;
    public string name;
    public RoomType type;
    public int floor;
    public int capacity;

    public int cleanliness = 70;  // 0..100
    public int condition = 80;    // исправность, 0..100

    public Room(string id, string name, RoomType type, int floor, int capacity)
    {
        this.id = id;
        this.name = name;
        this.type = type;
        this.floor = floor;
        this.capacity = capacity;
    }

    public void Worsen(int cleanDelta, int conditionDelta)
    {
        cleanliness = Mathf.Clamp(cleanliness - cleanDelta, 0, 100);
        condition = Mathf.Clamp(condition - conditionDelta, 0, 100);
    }

    public void Repair(int amount) => condition = Mathf.Clamp(condition + amount, 0, 100);
    public void Clean(int amount) => cleanliness = Mathf.Clamp(cleanliness + amount, 0, 100);
}