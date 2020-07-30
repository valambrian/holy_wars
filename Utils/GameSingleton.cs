using UnityEngine;

public class GameSingleton : MonoBehaviour
{
    private static GameSingleton _instance;
    private Loader _loader = new Loader();
    private ScenarioGenerator _scenarioGenerator = new ScenarioGenerator();

    public Game Game { get; private set; }

	void Awake ()
    {
        if (_instance == null)
        {
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }
        else
        {
            if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    public static GameSingleton Instance
    {
        get { return _instance; }
    }

    public void LoadGame(string scenario)
    {
        Game = _loader.LoadGameFromJSON(scenario);
    }

    public void NewGame()
    {
        GameData commonData = _loader.LoadGameDataFromJSON("common");
        Game = _scenarioGenerator.CreateScenario(commonData);
    }

    public void SaveGame()
    {
        _loader.SaveGame(Game);
    }

    public void LoadSavedGame()
    {
        if (_loader.DoesSaveFileExist())
        {
            Game game = _loader.LoadSavedGame();
            if (game != null)
            {
                Game = game;
            }
        }
    }

    public bool DoesSaveFileExist()
    {
        return _loader.DoesSaveFileExist();
    }



}
