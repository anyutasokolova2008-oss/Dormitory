using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Название игровой сцены
    [SerializeField] private string gameSceneName = "PlayTest";

    // Начать новую игру
    public void NewGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Продолжить игру
    public void ContinueGame()
    {
        Debug.Log("Продолжение игры пока не настроено.");
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