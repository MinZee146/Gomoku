
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using static System.Int32;
using Random = UnityEngine.Random;

public class BoardManager : Singleton<BoardManager>
{
    public RectTransform BoardRect => GetComponent<RectTransform>();
    public readonly string[,] BoardStatus = new string[Size, Size];
    public bool IsPlayerTurn;
    public bool IsGameOver;
    public bool IsGameStarted;
    public string CurrentDiff {get => _gameDifficulty.ToString();}
    
    public Vector2Int LastMove;

    public int Left = Size/2;
    public int Top = Size/2;
    public int Bottom = Size/2;
    public int Right = Size/2;
    
    private const int Size = 15;
    private const int WinCon = 5;
    private string _gameDifficulty;
    private int _searchDepth;
    
    private Tiles[,] _tilesArray = new Tiles[Size, Size];
    
    [SerializeField] private Tiles tilePrefab;
    
    private void Start()
    {
        FillGrid();
        StartNewSession();
    }

    public void StartNewSession()
    {
        if (GameManager.Instance.GameMode == "Single")
        {
            IsPlayerTurn = Random.value < 0.5f;
            _gameDifficulty = GameManager.Instance.GameLevel;
            IsGameStarted = true;
            NewGame();
        }
        else if (GameManager.Instance.GameMode == "Online")
        {
            NewMultiplayerGame();
        }
    }

    public void SwitchDepth(string gameDiff)
    {
        _searchDepth = gameDiff switch
        {
            "Easy" => 1,
            "Normal" => 2,
            "Hard" => 3,
            _ => 2
        };
        
        _gameDifficulty = gameDiff;
    }
    
    private void NewGame()
    {
        EmptyBoard();

        // set difficulty
        _searchDepth = _gameDifficulty switch
        {
            "Easy" => 1,
            "Normal" => 2,
            "Hard" => 3,
            _ => 2
        };
        
        AITurn();
    }

    public void ChangeDiff()
    {
        ResetBoard();
        AITurn();
    }

    public void NewMultiplayerGame()
    {
        EmptyBoard();
    }
    
    private void EmptyBoard()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                BoardStatus[x, y] = string.Empty;
            }
        }
    }
    
    public void ResetBoard()
    {
        EmptyBoard();

        foreach (var tile in _tilesArray)
        {
            tile.Reset();
        }

        Left = Top = Right = Bottom = Size / 2;
    }
    
    private void SetTile(Tiles tile)
    {
        _tilesArray[tile.Coordinates.x, tile.Coordinates.y] = tile;
    }
    
    private void FillGrid()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var newTile = Instantiate(tilePrefab, transform);
                newTile.Coordinates = new Vector2Int(y, x);
                newTile.name = $"{y},{x}";
                SetTile(newTile);
            }
        } 
    }

    public void SetCheckPoint(int x, int y, ref int left, ref int right, ref int top, ref int bottom)
    {
        if (x >= right)
            if (x + 1 is >= 0 and < Size) right = x + 1;
            else right = x;
        if (x <= left)
            if (x - 1 is >= 0 and < Size) left = x - 1;
            else left = x;
        if (y >= bottom)
            if (y + 1 is >= 0 and < Size) bottom = y + 1;
            else bottom = y;
        if (y <= top)
            if (y - 1 is >= 0 and < Size) top = y - 1;
            else top = y;
    }

    public async void AITurn()
    {
        if (!IsGameOver && !IsPlayerTurn)
        {
            var nextMove = await Task.Run(() => BestMove(BoardStatus));
            
            SpawnX(nextMove.x, nextMove.y);
            SetCheckPoint(nextMove.x, nextMove.y, ref Left, ref Right, ref Top, ref Bottom);
            CheckForGameOver();
        }
        
        UIManager.Instance.SetTurnText(true);
        IsPlayerTurn = true;
    }

    public void SpawnX(int x, int y)
    {
        BoardStatus[x, y] = "X";
        _tilesArray[x, y].SpawnBlack();
        LastMove = new Vector2Int(x, y);
        Debug.Log($"Last Move : {LastMove}");
    }

    public void CheckForGameOver()
    {
        if (!CheckForWinners(BoardStatus, LastMove.x, LastMove.y)) return;
        
        if (GameManager.Instance.GameMode == "Online")
            StartCoroutine(GameOverMessage());
        IsGameOver = true;
        StartCoroutine(GameOver());
    }

    private IEnumerator GameOverMessage()
    {
        yield return new WaitForSeconds(0.1f);
        
        ClientManager.Instance.SendMessageToServer("[GAMEOVER]");
    }
    
    public IEnumerator GameOver()
    {
        yield return new WaitForSeconds(1);
        UIManager.Instance.GameOver(BoardStatus[LastMove.x, LastMove.y]);
    }

    public bool CheckForWinners(string[,] boardStatus, int x, int y, bool playAnimations = true)
    {
        var currentPlayer = boardStatus[x, y];
        List<Vector2Int> winningTiles;

        winningTiles = CheckLineWin(currentPlayer, 1, 0, x, y);
        if (winningTiles.Count >= WinCon) { if (playAnimations) PlayWinningAnimations(winningTiles, currentPlayer); return true; }
        winningTiles = CheckLineWin(currentPlayer, 0, 1, x, y);
        if (winningTiles.Count >= WinCon) { if (playAnimations) PlayWinningAnimations(winningTiles, currentPlayer); return true; }

        winningTiles = CheckLineWin(currentPlayer, 1, 1, x, y);
        if (winningTiles.Count >= WinCon) { if (playAnimations) PlayWinningAnimations(winningTiles, currentPlayer); return true; }

        winningTiles = CheckLineWin(currentPlayer, 1, -1, x, y);
        if (winningTiles.Count >= WinCon) { if (playAnimations) PlayWinningAnimations(winningTiles, currentPlayer); return true; }

        return false;
    }

    private List<Vector2Int> CheckLineWin(string player, int dx, int dy, int x, int y)
    {
        var count = 1;
        var winningTiles = new List<Vector2Int> { new (x, y) };

        // Check in one direction (positive)
        for (var i = 1; i < WinCon; i++)
        {
            var nx = x + i * dx;
            var ny = y + i * dy;
            if (nx >= 0 && ny >= 0 && nx < Size && ny < Size && BoardStatus[nx, ny] == player)
            {
                count++;
                winningTiles.Add(new Vector2Int(nx, ny));
            }
            else break;
        }

        // Check in the other direction (negative)
        for (var i = 1; i < WinCon; i++)
        {
            var nx = x - i * dx;
            var ny = y - i * dy;
            if (nx >= 0 && ny >= 0 && nx < Size && ny < Size && BoardStatus[nx, ny] == player)
            {
                count++;
                winningTiles.Add(new Vector2Int(nx, ny));
            }
            else break;
        }

        if (count >= WinCon) return winningTiles;
        return new List<Vector2Int>();
    }
    
    private void PlayWinningAnimations(List<Vector2Int> winningTiles, string player)
    {
        if (winningTiles.Count > 0)
        {
            UnityMainThread.Instance.Enqueue(() =>
            {
                var sequence = DOTween.Sequence();
                foreach (var tile in winningTiles)
                {
                    sequence.Append(player == "O" ? _tilesArray[tile.x, tile.y].WhiteAnim() : _tilesArray[tile.x, tile.y].BlackAnim());
                }
            });
        }
    }
    
    private int Minimax(string[,] boardStatus, int depth, int alpha, int beta, bool isMaximizing, int left, int right, int top, int bottom, bool isGameOver)
    {
        if (depth == 0 || isGameOver) // add check game over here
        {
            var currentPlayer = isMaximizing ? "X" : "O";
            return Evaluate(boardStatus, currentPlayer, left, right, top, bottom);
        }

        if (isMaximizing)
        {
            var maxValue = MinValue;
            for (var y = top ; y <= bottom ; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    if (boardStatus[x, y] != string.Empty) continue;

                    boardStatus[x, y] = "X";
                    isGameOver = CheckForWinners(boardStatus, x, y);
                    var tempLeft = left;
                    var tempRight = right;
                    var tempTop = top;
                    var tempBottom = bottom;
                    SetCheckPoint(x, y, ref tempLeft, ref tempRight, ref tempTop, ref tempBottom);
                    var eval = Minimax(boardStatus, depth - 1, alpha, beta, false, tempLeft, tempRight, tempTop, tempBottom, isGameOver);
                    boardStatus[x, y] = string.Empty;

                    maxValue = maxValue > eval ? maxValue : eval;
                    alpha = Mathf.Max(alpha, eval);
                    if (alpha >= beta) break;
                    return maxValue;
                }
            }
        }
        
        var minValue = MaxValue;
        for (var y = top ; y <= bottom ; y++)
        {
            for (var x = left; x <= right; x++)
            {
                if (boardStatus[x, y] != string.Empty) continue;

                boardStatus[x, y] = "O";
                isGameOver = CheckForWinners(boardStatus, x, y);
                var tempLeft = left;
                var tempRight = right;
                var tempTop = top;
                var tempBottom = bottom;
                SetCheckPoint(x, y, ref tempLeft, ref tempRight, ref tempTop, ref tempBottom);
                var eval = Minimax(boardStatus, depth - 1, alpha, beta, true, tempLeft, tempRight, tempTop, tempBottom, isGameOver);
                boardStatus[x, y] = string.Empty;

                minValue = Mathf.Min(minValue, eval);
                beta = Mathf.Min(beta, eval);
                if (alpha >= beta) break;
            }
        }
        return minValue;
    }
    
    private int Evaluate(string[,] boardStatus, string player, int left, int right, int top, int bottom)
    {
        var threats = CountThreats(boardStatus, player, left, right, top, bottom);
        var opponent = player == "X" ? "O" : "X";
        var opponentThreats = CountThreats(boardStatus, opponent, left, right, top, bottom);

        return threats.Aggregate(0, (score, threat) => 
            score + GetThreatScore(threat.Key, threat.Value) - GetThreatScore(threat.Key, opponentThreats[threat.Key]));
    }
    
    private int GetThreatScore(string threatType, int count)
    {
        return threatType switch
        {
            "*****" =>  MaxValue,
            "_****_" => count * 100000000,
            "_**_**_" => count * 5000000,
            "?****?" => count * 5000000,
            "_***_" => count * 300,
            "?***?" => count * 50,
            "_**_" => count * 10,
            "*" => count,
            _ => 0
        };
    }
    
    private Dictionary<string, int> CountThreats(string[,] boardStatus, string player, int left, int right, int top, int bottom)
    {
        var threats = new Dictionary<string, int>
        {
            {"*****", 0},
            {"_****_", 0},
            {"?****?", 0},
            {"_***_", 0},
            {"?***?", 0},
            {"_**_", 0},
            {"*", 0}
        };

        for (var x = left; x <= right; x++)
        {
            for (var y = top; y <= bottom; y++)
            {
                if (boardStatus[x, y] != player) continue;
                threats["*"]++;

                if (EvaluateLine(player, boardStatus, x, y, 1, 0, left, right, top, bottom, ref threats) >=  WinCon) return threats;
                if (EvaluateLine(player, boardStatus, x, y, 0, 1, left, right, top, bottom, ref threats) >= WinCon) return threats;
                if (EvaluateLine(player, boardStatus, x, y, 1, 1, left, right, top, bottom, ref threats) >= WinCon) return threats;
                if (EvaluateLine(player, boardStatus, x, y, 1, -1, left, right, top, bottom, ref threats) >= WinCon) return threats;
            }
        }

        return threats;
    }
    
    private string GetThreatType(int count, int openEnds)
    {
        return (count, openEnds) switch
        {
            (>= 5, _) => "*****",
            (4, 2) => "_****_",
            (4, 1) => "?****?",
            (3, 2) => "_****_",
            (3, 1) => "?***?",
            (2, 2) => "_**_",
            _ => "*"
        };
    }
    
    private int EvaluateLine(string player, string[,] boardStatus, int x, int y, int dx, int dy,int left, int right,int top, int bottom, ref Dictionary<string, int> threats)
    {
        var emptyEnds = 0;
        var consecutiveLeft = 0;
        var consecutiveRight = 0;
        var op = player == "X" ? "O" : "X";

        // Check positive direction
        for (var i = 1; i < 5; i++)
        {
            var nx = x + i * dx;
            var ny = y + i * dy;
            
            if (nx > right || nx < left || ny > bottom || ny < top) break;
            
            if (boardStatus[nx, ny] == player)
            {
                consecutiveRight++;
            }
            else if (boardStatus[nx, ny] == op)
            {
                break;
            }
            else
            {
                emptyEnds++;
                break;
            }
        }
        
        // Check negative direction
        for (var i = 1; i < 5; i++)
        {
            var nx = x - i * dx;
            var ny = y - i * dy;

            if (nx > right || nx < left || ny > bottom || ny < top) break;

            if (boardStatus[nx, ny] == player)
            {
                consecutiveLeft++;
            }
            else if (boardStatus[nx, ny] == op)
            {
                break;
            }
            else
            {
                emptyEnds++;
                break;
            }
        }

        var totalConsecutive = consecutiveLeft + consecutiveRight + 1;
        threats[GetThreatType(totalConsecutive, emptyEnds)]++;
        return totalConsecutive;
    }
    
    private Vector2Int BestMove(string[,] boardStatus)
    {
        var forcedMove = DetectForcedMove(boardStatus,"X");
        if (forcedMove != null) return forcedMove.Value;
        
        var bestScore = MinValue;
        var bestMove = new Vector2Int();
        
        for (var y = Top ; y <= Bottom ; y++)
        {
            for (var x = Left; x <= Right; x++)
            {
                if (boardStatus[x, y] != string.Empty) continue;

                boardStatus[x, y] = "X";
                var score = Minimax(boardStatus, _searchDepth, MinValue, MaxValue, false, Left, Right, Top, Bottom, false);
                boardStatus[x, y] = string.Empty;

                if (score <= bestScore) continue;
                
                bestScore = score;
                bestMove = new Vector2Int(x, y);
            }
        }

        return bestMove;
    }
    
    private Vector2Int? DetectForcedMove(string[,] boardStatus, string player)
    {
        var opponent = player == "X" ? "O" : "X";
        Vector2Int winMove = default, defendMove = default, nextAttack = default, nextDefend = default;
        
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                if (boardStatus[x, y] != string.Empty) continue;
                
                boardStatus[x, y] = player;
                if (CheckForWinners(boardStatus, x, y, false))
                {
                    boardStatus[x, y] = string.Empty;
                    winMove = new Vector2Int(x, y);
                    break;
                }
                boardStatus[x, y] = string.Empty;

                // Check for a winning move for the opponent
                boardStatus[x, y] = opponent;
                if (CheckForWinners(boardStatus, x, y, false))
                {
                    boardStatus[x, y] = string.Empty;
                    defendMove = new Vector2Int(x, y);
                    break;
                }
                boardStatus[x, y] = string.Empty;
                
                //Check for next attack
                boardStatus[x, y] = player;
                if (CheckForAttacks(boardStatus, x, y))
                {
                    boardStatus[x, y] = string.Empty;
                    nextAttack = new Vector2Int(x, y);
                    break;
                }
                boardStatus[x, y] = string.Empty;
                
                //Check for next defend
                boardStatus[x, y] = opponent;
                if (CheckForAttacks(boardStatus, x, y))
                {
                    boardStatus[x, y] = string.Empty;
                    nextDefend = new Vector2Int(x, y);
                    break;
                }
                boardStatus[x, y] = string.Empty;
            }
        }

        if (winMove != default) return winMove;
        if (defendMove != default) return defendMove;
        if (nextAttack != default) return nextAttack;
        if (nextDefend != default) return nextDefend;
        
        return null;
    }
    
    private bool CheckForAttacks(string[,] boardStatus,int x,int y)
    {
        var currentPlayer = boardStatus[x,y];

        if (CheckLineAttack(currentPlayer, 1, 0, x, y) || // Check row
            CheckLineAttack(currentPlayer, 0, 1, x, y) || // Check column
            CheckLineAttack(currentPlayer, 1, 1, x, y) || // Check diagonal (top-left to bottom-right)
            CheckLineAttack(currentPlayer, 1, -1, x, y))  // Check diagonal (bottom-left to top-right)
        {
            return true;
        }
        
        return boardStatus.Cast<string>().All(value => !string.IsNullOrEmpty(value));
    }

    private bool CheckLineAttack(string player, int dx, int dy,int x, int y)
    {
        var count = 1;
        // Check in one direction (positive)
        for (var i = 1; i < WinCon - 1 ; i++)
        {
            var nx = x + i * dx;
            var ny = y + i * dy;

            if (nx >= 0 && ny >= 0 && nx < Size && ny < Size && BoardStatus[nx, ny] == player)
                count++;
            else
                break;
        }

        // Check in the other direction (negative)
        for (var i = 1; i < WinCon - 1; i++)
        {
            var nx = x - i * dx;
            var ny = y - i * dy;

            if (nx >= 0 && ny >= 0 && nx < Size && ny < Size && BoardStatus[nx, ny] == player)
                count++;
            else
                break;
        }

        return count >= WinCon - 1;
    }
}
