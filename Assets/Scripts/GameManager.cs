using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public string GameLevel;
    public string GameMode;
    
    protected override void Awake()
    {
        base.Awake();
        DOTween.Init();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
        if (BoardManager.Instance.IsGameStarted)
            UIManager.Instance.Clear();
        
        if (GameMode == "Online")
            UIManager.Instance.SetTurnText("Connecting ...");
        else if (GameMode == "Local")
            UIManager.Instance.SetTurnText(BoardManager.Instance.IsPlayerTurn ? "O's Turn" : "X's Turn");
        
        BoardManager.Instance.StartNewSession();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
