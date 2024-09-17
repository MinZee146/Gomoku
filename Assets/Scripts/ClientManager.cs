using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientManager : Singleton<ClientManager>
{
    public int Port;
    public string ServerIP = "0.tcp.ap.ngrok.io";
    
    private TcpClient _client;
    private NetworkStream _stream;
    private readonly byte[] _buffer = new byte[1024];

    public void ConnectToServer()
    {
        Debug.Log($"Port: {Port} {ServerIP}");
        try
        {
            _client = new TcpClient(ServerIP, Port);
            _stream = _client.GetStream();

            Debug.Log("Connected to server");

            _stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveMessage, null);
        }
        catch (Exception e)
        {
            UnityMainThread.Instance.Enqueue(() => Debug.LogError("Error connecting to server: " + e.Message));
        }
    }

    private void ReceiveMessage(IAsyncResult ar)
    {
        try
        {
            var bytesRead = _stream.EndRead(ar);
            Debug.Log($"bytesRead: {bytesRead}");
            if (bytesRead <= 0)
            {
                Debug.Log("Disconnected from server");
                return;
            }

            if (bytesRead > _buffer.Length)
            {
                Debug.LogError("Received more data than buffer can hold");
                return;
            }

            var message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
            Debug.Log("Received message from server: " + message);  

            HandleMessageReceived(message);

            _stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveMessage, null);
        }
        catch (Exception e)
        {
            UnityMainThread.Instance.Enqueue(() => Debug.LogError("Error receiving message from server: " + e.Message));
        }
    }

    public void SendMessageToServer(string message)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(message);
            _stream.Write(data, 0, data.Length);
            Debug.Log("Sent message to server: " + message);
        }
        catch (Exception e)
        {
            UnityMainThread.Instance.Enqueue(() => Debug.LogError("Error sending message to server: " + e.Message));
        }
    }

    private void OnDestroy()
    {
        _client?.Close();
    }

    private void HandleMessageReceived(string message)
    {
        var parts = message.Split(']');
        var tags = parts[0].TrimStart('[');
        var data = parts[1];
        
        Debug.Log(tags);
        switch (tags)
        {
            case "INITIALIZE"://Example Initialize message: [INITIALIZE] 1
                UnityMainThread.Instance.Enqueue(() =>
                {
                    var initializeParts = data.TrimStart(' ');

                    BoardManager.Instance.IsPlayerTurn = initializeParts == "1";
                    Debug.Log($"Player Turn: {initializeParts}");
                    BoardManager.Instance.NewMultiplayerGame();
                    UIManager.Instance.SetTurnText(BoardManager.Instance.IsPlayerTurn);
                });
                break;
            
            case "SPAWN"://Example Spawn message: [SPAWN] X at 7 7
                UnityMainThread.Instance.Enqueue(() =>
                {
                    var spawnParts = data.Split(' ');
                    var x = int.Parse(spawnParts[3]);
                    var y = int.Parse(spawnParts[4]);

                    BoardManager.Instance.SpawnX(x, y);
                    UIManager.Instance.SetTurnText(true);
                    BoardManager.Instance.IsPlayerTurn = true;
                });
                break;
  
            case "GAMEOVER":
                UnityMainThread.Instance.Enqueue(() =>
                {
                     BoardManager.Instance.CheckForWinners(BoardManager.Instance.BoardStatus, BoardManager.Instance.LastMove.x, BoardManager.Instance.LastMove.y);
                     StartCoroutine(BoardManager.Instance.GameOver());
                });
                break;
            
            case "RESTART_REQUEST":
                UnityMainThread.Instance.Enqueue(() =>
                {
                    UIManager.Instance.RecvRequest();
                });
                break;
            
            case "RESTART_YES":
                UnityMainThread.Instance.Enqueue(() =>
                {
                    UIManager.Instance.Rematch();
                });
                break;
            
            case "RESTART_NO":
                UnityMainThread.Instance.Enqueue(() =>
                {
                    UIManager.Instance.OpponentDenied();
                });
                break;
            
            case "RESTART_CANCEL":
                UnityMainThread.Instance.Enqueue(() =>
                {
                    UIManager.Instance.OpponentCancel();
                });
                break;
            
            case "DISCONNECTED":
                UnityMainThread.Instance.Enqueue(() =>
                {
                    UIManager.Instance.OpponentDisconnected();
                });
                break;
        }
    }
}