using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ResourcePanel panel;

    private ResourceManager resources;

    void Start()
    {
        resources = new ResourceManager();
        panel.Bind(resources);
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