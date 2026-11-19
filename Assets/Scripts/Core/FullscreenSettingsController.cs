
using UnityEngine;
using UnityEngine.UI;

public class FullscreenSettingsController : MonoBehaviour
{
    [Header("Переключатель полноэкранного режима")]
    [SerializeField] private Toggle fullscreenToggle;

    private void Start()
    {
        // Загружаем сохранённую настройку
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        // Применяем настройку
        Screen.fullScreen = isFullscreen;

        // Устанавливаем положение переключателя
        fullscreenToggle.isOn = isFullscreen;

        // Подписываемся на изменение переключателя
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        // Меняем режим отображения
        Screen.fullScreen = isFullscreen;

        // Сохраняем выбор игрока
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
