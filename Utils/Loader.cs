using System.IO;
using UnityEngine;

public class Loader
{
    public Game LoadGameFromJSON(string fileName)
    {
        GameData commonData = LoadGameDataFromJSON("common");
        if (commonData == null)
        {
            return null;
        }

        GameData scenarioData = LoadGameDataFromJSON(fileName);
        if (scenarioData == null)
        {
            return null;
        }

        scenarioData.races = commonData.races;
        scenarioData.units = commonData.units;
        if (scenarioData.factions == null)
        {
            scenarioData.factions = commonData.factions;
        }

        return new Game(scenarioData);
    }

    public GameData LoadGameDataFromJSON(string fileName)
    {
        TextAsset dataFile = Resources.Load(fileName) as TextAsset;
        if (dataFile == null)
        {
            Debug.LogError("Loader: JSON data file " + fileName + " not found");
            return null;
        }
        return JsonUtility.FromJson<GameData>(dataFile.text);
    }

    /// <summary>
    /// Returns application directory under which offline files can be stored
    /// </summary>
    /// <returns>
    /// Application directory under which offline files can be stored.
    /// </returns>
    private string GetDataDirectory()
    {
#if !UNITY_EDITOR
		return Application.persistentDataPath;	
#endif
        return Application.dataPath;
    }

    public void SaveGame(Game game)
    {
        GameData toSave = game.GetData();
        string data = JsonUtility.ToJson(toSave);
        string filename = GetDataDirectory() + "/save.txt";
        File.WriteAllText(filename, data);
        // BinaryFormatter bf = new BinaryFormatter();
        // FileStream file = File.Create(Application.persistentDataPath + "/save.data");
        // bf.Serialize(file, toSave);
        // file.Close();
    }

    public bool DoesSaveFileExist()
    {
        string filename = GetDataDirectory() + "/save.txt";
        return File.Exists(filename);
    }

    public Game LoadSavedGame()
    {
        if (!DoesSaveFileExist())
        {
            return null;
        }

        string data = File.ReadAllText(GetDataDirectory() + "/save.txt");
        if (data == null)
        {
            return null;
        }

        GameData gameData = JsonUtility.FromJson<GameData>(data);
        if (gameData == null)
        {
            return null;
        }

        return new Game(gameData);

        // FileStream file = File.Open(Application.persistentDataPath + "/save.data", FileMode.Open);
        // toLoad = (GameData)bf.Deserialize(file);
    }


}
