using System;
using System.Collections.Generic;

[Serializable]
public class Choice
{
    public string text;
    public float budgetCost;
    public int energyCost;
    public int timeCost;
    public List<Effect> effects = new List<Effect>();

    // Вариант доступен, если хватает энергии, времени и бюджет не уйдёт ниже -1000
    public bool IsAvailable(GameState state)
    {
        var r = state.resources;
        return r.CanAfford(budgetCost, energyCost, timeCost)
               && r.budget - budgetCost >= -1000f;
    }
}