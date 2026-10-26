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
}