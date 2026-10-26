using System.Collections.Generic;
using UnityEngine;

public enum EventKind { Household, Conflict, Discipline, Organizational, Personal, Random }

[CreateAssetMenu(fileName = "NewEvent", menuName = "Dormitory/Event")]
public class GameEvent : ScriptableObject
{
    public string eventId;
    public EventKind kind;
    public string title;
    [TextArea(3, 8)] public string description;
    public float baseWeight = 1f;
    public bool requiresStudent = true;
    public List<Choice> choices = new List<Choice>();
}