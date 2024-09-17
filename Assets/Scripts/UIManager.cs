using TMPro;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private CanvasGroup gameOverScreen;
    [SerializeField] private GameObject settingScreen;
    [SerializeField] private GameObject replayReqScreen;
    [SerializeField] private GameObject replayRecvScreen;
    [SerializeField] private GameObject opDisconnectScreen;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider difficultSlider;
    [SerializeField] private Image fillColor;
    
    private string _currentLevel;
    
    private void Start()
    {
        _currentLevel = GameManager.Instance.GameLevel;
        difficultSlider.onValueChanged.AddListener(OnSliderValueChanged);
        difficultSlider.value = _currentLevel switch
        {
            "Easy" => 0,
            "Normal" => 0.5f,
            "Hard" => 1,
            _ => 0.5f
        };
    }
    
    private void OnSliderValueChanged(float value)
    {
        var distanceToLeft = Mathf.Abs(value);
        var distanceToMiddle = Mathf.Abs(value - 0.5f);
        var distanceToRight = Mathf.Abs(value - 1f);

        if (distanceToLeft < distanceToMiddle && distanceToLeft < distanceToRight)
        {
            difficultSlider.value = 0;
            fillColor.color = Color.green;
            levelText.text = "Game Level: Easy";
            BoardManager.Instance.SwitchDepth("Easy");
        }
        else if (distanceToMiddle < distanceToLeft && distanceToMiddle < distanceToRight)
        {
            difficultSlider.value = 0.5f;
            fillColor.color = Color.yellow;
            levelText.text = "Game Level: Normal";
            BoardManager.Instance.SwitchDepth("Normal");
        }
        else
        {
            difficultSlider.value = 1f;
            fillColor.color = Color.red;
            levelText.text = "Game Level: Hard";
            BoardManager.Instance.SwitchDepth("Hard");
        }
        
        if (BoardManager.Instance.CurrentDiff == _currentLevel) return;
        SoundManager.Instance.PlayButtonSound();
        BoardManager.Instance.ChangeDiff();
        
        _currentLevel = BoardManager.Instance.CurrentDiff;
    }
    
    public void QuitGame()
    {
        SoundManager.Instance.PlayButtonSound();
        Clear();
        SceneManager.LoadScene("MainMenu");
        if (GameManager.Instance.GameMode != "Online") return;
        opDisconnectScreen.SetActive(false);
        ClientManager.Instance.SendMessageToServer("[DISCONNECTED]");
    }

    public void Replay()
    {
        SoundManager.Instance.PlayButtonSound();
        Clear();
        if (GameManager.Instance.GameMode == "Single")
            BoardManager.Instance.AITurn();
        else if (GameManager.Instance.GameMode == "Local")
            SetTurnText(BoardManager.Instance.IsPlayerTurn ? "O's Turn" : "X's Turn");
        else
        {
            ClientManager.Instance.SendMessageToServer("[RESTART_REQUEST]");
            replayReqScreen.SetActive(true);
        }
    }

    public void HideUI()
    {
        gameOverScreen.alpha = 0;
    }
    
    public void ShowUI()
    {
        gameOverScreen.alpha = 1;
    }

    public void Clear()
    {
        gameOverScreen.interactable = false;
        gameOverScreen.blocksRaycasts = false;
        BoardManager.Instance.ResetBoard();
        BoardManager.Instance.IsGameOver = false;
        gameOverScreen.alpha = 0;
    }

    public void OpenSettings()
    {
        settingScreen.SetActive(true);
        SoundManager.Instance.PlayButtonSound();
    }

    public void CloseSettings()
    {
        settingScreen.SetActive(false);
        SoundManager.Instance.PlayButtonSound();
    }

    public void GameOver(string winner)
    {
        winnerText.text = winner switch
        {
            "Tied" => "Game tied",
            "O" => "You won !",
            "X" => "You lost ...",
            _ => "Tied"
        };
        turnText.text = "Game Over";
        
        gameOverScreen.interactable = true;
        gameOverScreen.blocksRaycasts = true;
        gameOverScreen.DOFade(1, 0.5f);

        if (winner == "O")
        {
            SoundManager.Instance.PlayVictorySound();
        }
        else
        {
            SoundManager.Instance.PlayLoseSound();
        }
    }

    public void SetTurnText(bool isPlayerTurn)
    {
        turnText.text = isPlayerTurn ? "Your turn" : "Opponent turn!";
    }
    
    public void SetTurnText(string text)
    {
        turnText.text = text;
    }
    
    public void CancelRequest()
    {
        SoundManager.Instance.PlayButtonSound();
        replayReqScreen.SetActive(false);
        SceneManager.LoadScene("MainMenu");
        ClientManager.Instance.SendMessageToServer("[RESTART_CANCEL]");
    }
    
    public void RecvRequest()
    {
        Clear();
        replayRecvScreen.SetActive(true);
    }

    public void Deny()
    {
        SoundManager.Instance.PlayButtonSound();
        replayRecvScreen.SetActive(false);
        SceneManager.LoadScene("MainMenu");
        ClientManager.Instance.SendMessageToServer("[RESTART_NO]");
    }

    public void Accept()
    {
        SoundManager.Instance.PlayButtonSound();
        replayRecvScreen.SetActive(false);
        SetTurnText(BoardManager.Instance.IsPlayerTurn);
        ClientManager.Instance.SendMessageToServer("[RESTART_YES]");
    }

    public void Rematch()
    {
        replayReqScreen.SetActive(false);
        SetTurnText(BoardManager.Instance.IsPlayerTurn);
    }

    public void OpponentDenied()
    {
        Clear();
        replayReqScreen.SetActive(false);
        opDisconnectScreen.SetActive(true);
    }
    
    public void OpponentDisconnected()
    {
        Clear();
        opDisconnectScreen.SetActive(true);
    }

    public void OpponentCancel()
    {
        replayRecvScreen.SetActive(false);
        opDisconnectScreen.SetActive(true);
    }
}
