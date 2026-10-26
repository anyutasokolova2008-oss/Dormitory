using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ResourcePanel panel;
    [SerializeField] private GameEvent testEvent;
    [SerializeField] private int testChoiceIndex = 0;

    private GameState state;
    private EventManager eventManager;

    void Start()
    {
        state = GameState.CreateNew();
        eventManager = new EventManager();
        panel.Bind(state.resources);
    }

    [ContextMenu("Тест: показать событие")]
    void TestShowEvent()
    {
        Debug.Log($"{testEvent.title}: {testEvent.description}");
        for (int i = 0; i < testEvent.choices.Count; i++)
            Debug.Log($"  [{i}] {testEvent.choices[i].text}");
    }

    [ContextMenu("Тест: применить выбранный вариант")]
    void TestApplyChoice()
    {
        var student = state.students[0];
        bool ok = eventManager.TryApplyChoice(state, testEvent, testChoiceIndex, student, null);
        Debug.Log(ok ? "Вариант применён" : "Вариант недоступен");
    }

    [ContextMenu("Тест: вывести всех студентов")]
    void TestListStudents()
    {
        foreach (var st in state.students)
            Debug.Log($"{st.name} ({st.roomId}): дисциплина {st.discipline}, " +
                      $"настроение {st.mood}, отношение {st.attitude}, состояние {st.GetState()}");
    }
}