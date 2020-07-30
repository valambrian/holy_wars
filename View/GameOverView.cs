using System;
using UnityEngine;

public class GameOverView : MonoBehaviour
{
    public event EventHandler<EventArgs> MessageAcknowledged;

    [SerializeField]
    private TextMesh _messageField;

    [SerializeField]
    private MouseClickListener _doneButton;

    void Start()
    {
        _doneButton.MouseClickDetected += OnDoneButtonPressed;
    }

    public void SetGameWon(bool hellYes)
    {
        if (hellYes)
        {
            _messageField.color = Color.green;
            _messageField.text = "YOU WON";
        }
        else
        {
            _messageField.color = Color.red;
            _messageField.text = "YOU LOST";
        }
    }


    private void OnDoneButtonPressed(object sender, System.EventArgs args)
    {
        if (MessageAcknowledged != null)
        {
            MessageAcknowledged(this, EventArgs.Empty);
        }
        _doneButton.MouseClickDetected -= OnDoneButtonPressed;
        Destroy(gameObject);
    }

}
