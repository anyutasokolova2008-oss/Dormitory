
using UnityEngine;

public class AboutPanelController : MonoBehaviour
{
    [SerializeField] private GameObject aboutPanel;

    public void OpenAbout()
    {
        aboutPanel.SetActive(true);
    }

    public void CloseAbout()
    {
        aboutPanel.SetActive(false);
    }
}
