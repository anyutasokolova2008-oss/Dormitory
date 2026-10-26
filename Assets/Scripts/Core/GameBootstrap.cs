using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private ResourceManager resources;

    void Start()
    {
        resources = new ResourceManager();
        resources.OnChanged += type => Debug.Log($"Изменился ресурс {type}. " +
            $"Бюджет: {resources.budget}, репутация: {resources.reputation}, " +
            $"энергия: {resources.energy}, время: {resources.time}");
    }

    [ContextMenu("Тест: штраф студенту")]
    void TestFine()
    {
        resources.Change(ResourceType.Budget, 50);
        resources.Change(ResourceType.Energy, -5);
    }

    [ContextMenu("Тест: репутация выше максимума")]
    void TestClamp()
    {
        resources.Change(ResourceType.Reputation, 999);
    }
}