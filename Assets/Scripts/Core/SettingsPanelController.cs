
using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Окно настроек")]
    [SerializeField] private GameObject settingsPanel;

    // Открытие окна настроек
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // Закрытие окна настроек
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}
