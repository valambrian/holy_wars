using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    [SerializeField]
    private Button _continueButton;

    void Start()
    {
        _continueButton.interactable = GameSingleton.Instance.DoesSaveFileExist();
    }

    public void Continue()
    {
        GameSingleton.Instance.LoadSavedGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene("StrategicMap");
    }

    public void Scenario()
    {
        GameSingleton.Instance.LoadGame("first");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Setup");
    }

    public void Random()
    {
        GameSingleton.Instance.NewGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Setup");
    }

    public void Quit()
    {
        Application.Quit();
    }

}
