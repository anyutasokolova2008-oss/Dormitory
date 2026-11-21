using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    // Название игровой сцены
    [SerializeField] private string gameSceneName = "PlayTest";

    // Кнопка «Продолжить». Если не заполнена в Inspector,
    // найдём её по имени объекта ContinueButton.
    [SerializeField] private Button continueButton;

    void Start()
    {
        if (continueButton == null)
        {
            var go = GameObject.Find("ContinueButton");
            if (go != null) continueButton = go.GetComponent<Button>();
        }

        // Нет сохранения — кнопка неактивна.
        if (continueButton != null)
            continueButton.interactable = SaveManager.HasSave();
    }

    // Начать новую игру
    public void NewGame()
    {
        GameSession.RequestNewGame();
        SceneManager.LoadScene(gameSceneName);
    }

    // Продолжить игру
    public void ContinueGame()
    {
        if (!SaveManager.HasSave())
        {
            Debug.LogWarning("[MainMenu] Файл сохранения не найден — продолжить нельзя.");
            if (continueButton != null) continueButton.interactable = false;
            return;
        }

        GameSession.RequestContinue();
        SceneManager.LoadScene(gameSceneName);
    }

    // Выход из игры
    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
