using System;
using System.Collections.Generic;
using UnityEngine;

public enum StudentStat { Discipline, Mood, Grades, Attitude }

// Состояния из таблицы 2.3
public enum StudentState { Neutral, Content, Dissatisfied, Conflicted, Critical }

[Serializable]
public class Student
{
    public string id;
    public string name;
    public int course;
    public string roomId;

    public int discipline = 50;   // 0..100
    public int mood = 50;         // 0..100
    public int grades = 50;       // 0..100
    public int attitude = 0;      // -100..100 (отношение к коменданту)

    public int lowMoodDays;       // сколько дней подряд настроение ниже 10
    public List<string> history = new List<string>();

    public Student(string id, string name, int course, string roomId)
    {
        this.id = id;
        this.name = name;
        this.course = course;
        this.roomId = roomId;
    }

    // Формула (2.2): значение не выходит за допустимые границы
    public void ApplyEffect(StudentStat stat, int delta)
    {
        switch (stat)
        {
            case StudentStat.Discipline: discipline = Mathf.Clamp(discipline + delta, 0, 100); break;
            case StudentStat.Mood: mood = Mathf.Clamp(mood + delta, 0, 100); break;
            case StudentStat.Grades: grades = Mathf.Clamp(grades + delta, 0, 100); break;
            case StudentStat.Attitude: attitude = Mathf.Clamp(attitude + delta, -100, 100); break;
        }
    }

    public void AddToHistory(string record) => history.Add(record);

    // Условия из таблицы 2.3, проверка с самого тяжёлого состояния
    public StudentState GetState()
    {
        if (lowMoodDays >= 3) return StudentState.Critical;
        if (mood < 25 && attitude < -40) return StudentState.Conflicted;
        if (mood < 40 || attitude < -20) return StudentState.Dissatisfied;
        if (mood >= 70 && attitude >= 20) return StudentState.Content;
        return StudentState.Neutral;
    }
}