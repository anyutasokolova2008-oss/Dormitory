using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DelayedSpec
{
    public GameEvent ev;
    public int daysLater = 3;
    [Range(0f, 1f)] public float chance = 1f;   // 0.3 = 30 %
}

[Serializable]
public class Choice
{
    public string text;
    public float budgetCost;
    public int energyCost;
    public int timeCost;
    public List<Effect> effects = new List<Effect>();
    public List<DelayedSpec> delayed = new List<DelayedSpec>();

    public bool IsAvailable(GameState state)
    {
        var r = state.resources;
        return r.CanAfford(budgetCost, energyCost, timeCost)
               && r.budget - budgetCost >= -200f;
    }
}