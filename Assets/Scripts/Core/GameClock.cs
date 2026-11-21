using UnityEngine;

// Единые игровые часы. Пока время идёт только от действий (решения в событиях),
// но именно этот класс будет единственным источником времени для света,
// жильцов и распорядка дня на следующих этапах.
public static class GameClock
{
    public const float DayStartHour = 8f;   // день начинается в 08:00
    public const float DayEndHour = 22f;    // к 22:00 наступает ночь
    public const int MaxHours = 14;         // столько часов доступно за день (ресурс «Время»)

    public static float Hour { get; private set; } = DayStartHour;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode()
    {
        Hour = DayStartHour;
    }

    public static void ResetToDayStart()
    {
        Hour = DayStartHour;
    }

    public static void Advance(float hours)
    {
        if (hours <= 0f) return;
        Hour = Mathf.Clamp(Hour + hours, 0f, 24f);
    }

    public static void SetHour(float hour)
    {
        Hour = Mathf.Clamp(hour, 0f, 24f);
    }

    // Сколько сейчас времени, если израсходовано spent часов.
    public static float HourFromSpent(int spentHours)
    {
        return Mathf.Clamp(DayStartHour + spentHours, 0f, 24f);
    }
}
