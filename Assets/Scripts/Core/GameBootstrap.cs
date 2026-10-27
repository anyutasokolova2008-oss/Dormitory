using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ResourcePanel panel;
    [SerializeField] private EventWindow eventWindow;
    [SerializeField] private GameEvent testEvent;

    private GameState state;
    private EventManager eventManager;

    void Start()
    {
        state = GameState.CreateNew();
        eventManager = new EventManager();
        panel.Bind(state.resources);
        eventWindow.Hide();
    }

    [ContextMenu("Тест: показать окно события")]
    void TestShowWindow()
    {
        var student = state.students[0];
        eventWindow.Show(testEvent, state, index =>
        {
            bool ok = eventManager.TryApplyChoice(state, testEvent, index, student, null);
            Debug.Log(ok ? $"Выбран вариант {index}" : "Вариант недоступен");
        });
    }

    [ContextMenu("Тест: вывести всех студентов")]
    void TestListStudents()
    {
        foreach (var st in state.students)
            Debug.Log($"{st.name} ({st.roomId}): дисциплина {st.discipline}, " +
                      $"настроение {st.mood}, отношение {st.attitude}, состояние {st.GetState()}");
    }
}