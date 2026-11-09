using System.Collections.Generic;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ResourcePanel panel;
    [SerializeField] private EventWindow eventWindow;
    [SerializeField] private List<GameEvent> allEvents = new List<GameEvent>();

    private GameState state;
    private EventManager eventManager;
    private DayCycleManager dayCycle;

    void Start()
    {
        StartNewGame();
    }

    void StartNewGame()
    {
        state = GameState.CreateNew();
        var rng = new System.Random();
        eventManager = new EventManager(rng);
        dayCycle = new DayCycleManager(state, eventManager, allEvents, rng);
        panel.Bind(state.resources);
        StartNextDay();
    }

    void StartNextDay()
    {
        dayCycle.StartDay();
        ShowNextEvent();
    }

    void ShowNextEvent()
    {
        if (!dayCycle.HasNextEvent)
        {
            FinishDay();
            return;
        }

        var de = dayCycle.NextEvent();

        string context = $"День {state.day} из {DayCycleManager.LastDay}";
        if (de.student != null) context += $"\nПроживающий: {de.student.name}";
        if (de.room != null) context += $"\nМесто: {de.room.name}";

        eventWindow.Show(de.ev, state, index =>
        {
            eventManager.TryApplyChoice(state, de.ev, index, de.student, de.room);

            if (state.resources.IsDefeated()) FinishDay(); // поражение посреди дня
            else ShowNextEvent();
        }, context);
    }

    void FinishDay()
    {
        int dayNumber = state.day;
        string report = dayCycle.EndDay();

        if (dayCycle.IsOver)
        {
            float rating = dayCycle.CalculateRating();
            string ending = dayCycle.Defeated ? "Увольнение" : DayCycleManager.GetEnding(rating);
            ShowInfo($"Концовка: {ending}",
                     report + $"\nИтоговый рейтинг: {rating:0} из 100",
                     "Начать заново", StartNewGame);
        }
        else
        {
            ShowInfo($"Итоги дня {dayNumber}", report, "Следующий день", StartNextDay);
        }
    }

    // Временный экран: карточка с одной кнопкой
    void ShowInfo(string title, string text, string buttonText, System.Action onClose)
    {
        var info = ScriptableObject.CreateInstance<GameEvent>();
        info.title = title;
        info.description = text;
        info.choices.Add(new Choice { text = buttonText });
        eventWindow.Show(info, state, _ => onClose());
    }
}