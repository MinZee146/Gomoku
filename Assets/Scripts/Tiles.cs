using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class Tiles : MonoBehaviour
{
    public Vector2Int Coordinates { get; set; }
    private bool IsOccupied { get; set; }
    
    [SerializeField] private Button button;
    [SerializeField] private Sprite white;
    [SerializeField] private Sprite black;
    
    public void OnClick()
    {
        if (GameManager.Instance.GameMode == "Local" && !BoardManager.Instance.IsGameOver && !IsOccupied)
        {
            if (BoardManager.Instance.IsPlayerTurn)
            {
                SpawnWhite();
                BoardManager.Instance.IsPlayerTurn = false;
                BoardManager.Instance.BoardStatus[Coordinates.x, Coordinates.y] = "O";
                BoardManager.Instance.LastMove = Coordinates;
                BoardManager.Instance.CheckForGameOver();
                BoardManager.Instance.SetCheckPoint(Coordinates.x,Coordinates.y,ref BoardManager.Instance.Left,ref BoardManager.Instance.Right,ref BoardManager.Instance.Top,ref BoardManager.Instance.Bottom);
                UIManager.Instance.SetTurnText("X's Turn");
                return;
            }
            
            SpawnBlack();
            BoardManager.Instance.IsPlayerTurn = true;
            BoardManager.Instance.BoardStatus[Coordinates.x, Coordinates.y] = "X";
            BoardManager.Instance.LastMove = Coordinates;
            BoardManager.Instance.CheckForGameOver();
            BoardManager.Instance.SetCheckPoint(Coordinates.x,Coordinates.y,ref BoardManager.Instance.Left,ref BoardManager.Instance.Right,ref BoardManager.Instance.Top,ref BoardManager.Instance.Bottom);
            UIManager.Instance.SetTurnText("O's Turn");
            return;
        }
        
        if (!BoardManager.Instance.IsPlayerTurn || BoardManager.Instance.IsGameOver || IsOccupied) return;
        
        SpawnWhite();
            
        BoardManager.Instance.IsPlayerTurn = false;
        BoardManager.Instance.BoardStatus[Coordinates.x, Coordinates.y] = "O";
        BoardManager.Instance.LastMove = Coordinates;
        if (GameManager.Instance.GameMode == "Online")
            ClientManager.Instance.SendMessageToServer($"[SPAWN] X at {Coordinates.x} {Coordinates.y}");
        BoardManager.Instance.CheckForGameOver();
        BoardManager.Instance.SetCheckPoint(Coordinates.x,Coordinates.y,ref BoardManager.Instance.Left,ref BoardManager.Instance.Right,ref BoardManager.Instance.Top,ref BoardManager.Instance.Bottom);
        
        UIManager.Instance.SetTurnText(false);
        
        if (GameManager.Instance.GameMode == "Single")
            StartCoroutine(WaitForUI());
    }

    private IEnumerator WaitForUI()
    {
        yield return new WaitForSeconds(0.3f);
        
        BoardManager.Instance.AITurn();
    }

    //Spawns player's piece
    private void SpawnWhite()
    { 
        button.image.sprite = white;
        Animation(button.image);
        IsOccupied = true;
        
        SoundManager.Instance.PlaySpawnSound();
    }

    //Spawns bot's piece
    public void SpawnBlack()
    {
        // if (!IsOccupied)
        // {
            button.image.sprite = black;
            Animation(button.image);
            IsOccupied = true;
            
        SoundManager.Instance.PlaySpawnSound();
    }

    public void Reset()
    {
        button.image.sprite = null;
        IsOccupied = false;
    }

    private void Animation(Image image)
    {
        var sequence = DOTween.Sequence()
            .Append(image.transform.DOScale(image.transform.localScale * 1.2f, 0.1f).SetEase(Ease.InOutSine))
            .Append(image.transform.DOScale(image.transform.localScale, 0.1f).SetEase(Ease.InOutSine));
    }

    public Sequence WhiteAnim()
    {
        return DOTween.Sequence()
            .Append(button.image.transform.DOScale(button.image.transform.localScale * 1.1f, 0.1f).SetEase(Ease.InOutSine))
            .Append(button.image.transform.DOScale(button.image.transform.localScale, 0.1f).SetEase(Ease.InOutSine));
    }

    public Sequence BlackAnim()
    {
        return DOTween.Sequence()
            .Append(button.image.transform.DOScale(button.image.transform.localScale * 1.1f, 0.1f).SetEase(Ease.InOutSine))
            .Append(button.image.transform.DOScale(button.image.transform.localScale, 0.1f).SetEase(Ease.InOutSine));
    }
}
