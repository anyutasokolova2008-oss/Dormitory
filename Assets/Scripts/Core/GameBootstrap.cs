using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ResourcePanel panel;

    private GameState state;

    void Start()
    {
        state = GameState.CreateNew();
        panel.Bind(state.resources);
    }

    [ContextMenu("Тест: штраф студенту")]
    void TestFine()
    {
        state.resources.Change(ResourceType.Budget, 50);
        state.resources.Change(ResourceType.Energy, -5);
        state.GetStudent("s1").ApplyEffect(StudentStat.Discipline, 10);
        state.GetStudent("s1").ApplyEffect(StudentStat.Attitude, -20);
    }

    [ContextMenu("Тест: вывести всех студентов")]
    void TestListStudents()
    {
        foreach (var st in state.students)
            Debug.Log($"{st.name} ({st.roomId}): дисциплина {st.discipline}, " +
                      $"настроение {st.mood}, отношение {st.attitude}, состояние {st.GetState()}");
    }

    [ContextMenu("Тест: испортить отношение")]
    void TestBadAttitude()
    {
        var st = state.GetStudent("s2");
        st.ApplyEffect(StudentStat.Mood, -40);
        st.ApplyEffect(StudentStat.Attitude, -50);
        Debug.Log($"{st.name}: состояние {st.GetState()}");
    }
}