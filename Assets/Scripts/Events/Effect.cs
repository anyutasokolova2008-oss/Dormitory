using System;

public enum EffectTarget
{
    Budget, Reputation, Energy, Time,
    StudentDiscipline, StudentMood, StudentGrades, StudentAttitude,
    RoomCleanliness, RoomCondition
}

[Serializable]
public class Effect
{
    public EffectTarget target;
    public float value;

    public void Apply(GameState state, Student student, Room room)
    {
        var r = state.resources;
        switch (target)
        {
            case EffectTarget.Budget: r.Change(ResourceType.Budget, value); break;
            case EffectTarget.Reputation: r.Change(ResourceType.Reputation, value); break;
            case EffectTarget.Energy: r.Change(ResourceType.Energy, value); break;
            case EffectTarget.Time: r.Change(ResourceType.Time, value); break;

            case EffectTarget.StudentDiscipline: student?.ApplyEffect(StudentStat.Discipline, (int)value); break;
            case EffectTarget.StudentMood: student?.ApplyEffect(StudentStat.Mood, (int)value); break;
            case EffectTarget.StudentGrades: student?.ApplyEffect(StudentStat.Grades, (int)value); break;
            case EffectTarget.StudentAttitude: student?.ApplyEffect(StudentStat.Attitude, (int)value); break;

            case EffectTarget.RoomCleanliness: room?.Clean((int)value); break;
            case EffectTarget.RoomCondition: room?.Repair((int)value); break;
        }
    }
}