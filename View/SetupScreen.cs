using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetupScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject _notification;

    [SerializeField]
    Dropdown[] players;

    private Game _model;

    void Start ()
    {
        _model = GameSingleton.Instance.Game;
        _notification.SetActive(false);
    }

    public void Play()
    {
        List<Faction> factions = _model.GetAllFactions();
        for (int i = 0; i < players.Length; i++)
        {
            if (factions[i].IsPlayable())
            {
                if (players[i].value == 0)
                {
                    factions[i].SetIsPC(true);
                    factions[i].SetAILevel(2);
                }
                else
                {
                    factions[i].SetIsPC(false);
                    factions[i].SetAILevel(players[i].value - 1);
                }
            }
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("StrategicMap");
    }

    public void CloseHelpWindow()
    {
        _notification.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonUp("Help"))
        {
            _notification.SetActive(true);
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

}
