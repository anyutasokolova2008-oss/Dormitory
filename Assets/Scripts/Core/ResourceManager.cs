using System;
using UnityEngine;

public enum ResourceType { Budget, Reputation, Energy, Time }

[Serializable]
public class ResourceManager
{
    // Начальные значения из таблицы 1.2 (позже перенесём в BalanceConfig)
    public float budget = 1000f;
    public int reputation = 50;
    public int energy = 100;
    public int time = 14;

    // Интерфейс подпишется на это событие и обновит цифры на экране
    public event Action<ResourceType> OnChanged;

    public void Change(ResourceType type, float delta)
    {
        switch (type)
        {
            case ResourceType.Budget:
                budget += delta; // нижней границы нет: долг допустим до -1000
                break;
            case ResourceType.Reputation:
                reputation = Mathf.Clamp(reputation + Mathf.RoundToInt(delta), 0, 100); // формула 2.2
                break;
            case ResourceType.Energy:
                energy = Mathf.Clamp(energy + Mathf.RoundToInt(delta), 0, 100);
                break;
            case ResourceType.Time:
                time = Mathf.Clamp(time + Mathf.RoundToInt(delta), 0, 14);
                break;
        }
        OnChanged?.Invoke(type);
    }

    public bool CanAfford(float budgetCost, int energyCost, int timeCost)
    {
        return energy >= energyCost && time >= timeCost; // бюджет проверим отдельно, он может уходить в минус
    }

    public bool IsDefeated()
    {
        return reputation < 15 || budget < -200f;
    }
}