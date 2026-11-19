
using System;
using UnityEngine;

public class MainMenuBackground : MonoBehaviour
{
    [Header("Фоны главного меню")]
    [SerializeField] private GameObject dayBackground;
    [SerializeField] private GameObject nightBackground;

    private void Start()
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        // Получаем текущее время компьютера игрока.
        int currentHour = DateTime.Now.Hour;

        // С 06:00 до 17:59 показываем дневной фон.
        bool isDay = currentHour >= 6 && currentHour < 18;

        dayBackground.SetActive(isDay);
        nightBackground.SetActive(!isDay);
    }
}
