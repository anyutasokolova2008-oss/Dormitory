using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventWindow : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private Button buttonPrefab;

    // Открыто ли сейчас окно события/отчёта.
    // Нужно, чтобы не сохранять игру в момент, когда событие уже достали
    // из очереди, но решение ещё не принято (иначе событие потеряется).
    public bool IsOpen => root != null && root.activeSelf;

    public void Show(GameEvent ev, GameState state, Action<int> onChosen, string context = null)
    {
        Clear();
        titleText.text = ev.title;
        descriptionText.text = string.IsNullOrEmpty(context)
            ? ev.description
            : context + "\n\n" + ev.description;

        for (int i = 0; i < ev.choices.Count; i++)
        {
            int index = i; // копия для кнопки, иначе все кнопки получат последний номер
            var choice = ev.choices[i];

            var btn = Instantiate(buttonPrefab, buttonsContainer);
            btn.GetComponentInChildren<TMP_Text>().text = FormatChoice(choice);
            btn.interactable = choice.IsAvailable(state);
            btn.onClick.AddListener(() =>
            {
                Hide();
                onChosen(index);
            });
        }

        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    void Clear()
    {
        foreach (Transform child in buttonsContainer)
            Destroy(child.gameObject);
    }

    string FormatChoice(Choice c)
    {
        var costs = new List<string>();
        if (c.budgetCost != 0) costs.Add($"{c.budgetCost:0} руб.");
        if (c.energyCost != 0) costs.Add($"энергия {c.energyCost}");
        if (c.timeCost != 0) costs.Add($"время {c.timeCost} ч");
        return costs.Count == 0 ? c.text : $"{c.text}  ({string.Join(", ", costs)})";
    }
}