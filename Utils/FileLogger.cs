using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using UnityEngine;

public class FileLogger
{
    private const string TRACE = "";
    private const string WARNING = "WARNING: ";
    private const string ERROR = "ERROR: ";
    private const string LOGGER_CONFIG_FILE = "file.logger.config";
    private const string LOG_FILE = "log.txt";

    private static List<string> _topics = null;
    private static StreamWriter _logFile = null;

    public static string GetAppDirectory()
    {
#if !UNITY_EDITOR
			return Application.persistentDataPath;	
#endif
        return Application.dataPath;
    }

    private static string GetConfigFileName()
    {
        return Path.Combine(GetAppDirectory(), LOGGER_CONFIG_FILE);
    }

    private static string GetLogFileName()
    {
        return Path.Combine(GetAppDirectory(), LOG_FILE);
    }

    private static void Initialize()
    {
        _topics = new List<string>();

        try
        {
            StreamReader sr = File.OpenText(GetConfigFileName());
            string line = sr.ReadLine();
            while (line != null)
            {
                _topics.Add(line.ToUpper());
                line = sr.ReadLine();
            }
        }
        catch (FileNotFoundException ex)
        {
            Debug.Log(ex.Message);
            Debug.Log("Logging is disabled");
        }
        catch (IsolatedStorageException ex)
        {
            Debug.Log(ex.Message);
            Debug.Log("Logging is disabled");
        }

        _logFile = File.CreateText(GetLogFileName());
        _logFile.AutoFlush = true;
    }

    private static bool IsTopicActive(string topic)
	{
        if (_topics == null)
        {
            Initialize();
        }
        return _topics.Contains(topic);
	}

	private static void Log(string level, string topic, string message, bool forceMessage = true)
	{
        string uppercaseTopic = topic.ToUpper();
        if (forceMessage || IsTopicActive(uppercaseTopic))
        {
            _logFile.WriteLine(Time.time.ToString() + " s: " +  level + "<" + uppercaseTopic + "> " + message);
        }
    }
	
	public static void Trace(string topic, string message)
	{
        Log(TRACE, topic, message, false);
	}

    public static void Warn(string topic, string message)
    {
        Log(WARNING, topic, message);
    }

    public static void Error(string topic, string message)
    {
        Log(ERROR, topic, message);
    }

}
