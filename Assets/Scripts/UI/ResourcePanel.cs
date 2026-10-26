using TMPro;
using UnityEngine;

public class ResourcePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private ResourceManager resources;

    public void Bind(ResourceManager manager)
    {
        if (resources != null) resources.OnChanged -= OnResourceChanged;
        resources = manager;
        resources.OnChanged += OnResourceChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (resources != null) resources.OnChanged -= OnResourceChanged;
    }

    void OnResourceChanged(ResourceType type) => Refresh();

    void Refresh()
    {
        label.text =
            $"Бюджет: {resources.budget:0} руб.\n" +
            $"Репутация: {resources.reputation}/100\n" +
            $"Энергия: {resources.energy}/100\n" +
            $"Время: {resources.time} ч";
    }
}