using System;

[Serializable]
public class DelayedEvent
{
    public string eventId;   // строкой, чтобы потом сохранять в JSON
    public int dueDay;       // день наступления
    public string studentId;
    public string roomId;
}