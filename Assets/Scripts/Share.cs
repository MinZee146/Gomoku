using System.Collections;
using System.IO;
using UnityEngine;

public class Share : Singleton<Share>
{
    [SerializeField] RenderTexture boardTexture;
    [SerializeField] Camera boardCamera;
    [SerializeField] Canvas boardCanvas;
    
    public void ShareBoard()
    {
        // StartCoroutine(TakePhotoAndShare());
        StartCoroutine(TakeScreenshotAndShare(boardTexture));
    }
    
    //Take end game board's photo without render texture
    private IEnumerator TakePhotoAndShare()
    {
        UIManager.Instance.HideUI();
        
        yield return new WaitForEndOfFrame();

        var gameBoardRectTransform = BoardManager.Instance.BoardRect;

        var size = Vector2.Scale(gameBoardRectTransform.rect.size, gameBoardRectTransform.lossyScale);
        var rect = new Rect(gameBoardRectTransform.position.x, gameBoardRectTransform.position.y, size.x, size.y);
        rect.x -= (gameBoardRectTransform.pivot.x * size.x);
        rect.y -= ((1.0f - gameBoardRectTransform.pivot.y) * size.y);

        var ss = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        ss.ReadPixels(rect, 0, 0);
        ss.Apply();

        var filePath = Path.Combine(Application.temporaryCachePath, "shared img.png");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());

        // To avoid memory leaks
        Destroy(ss);
        
        UIManager.Instance.ShowUI();
        new NativeShare().AddFile(filePath).Share();
    }
    
    //Take end game board's photo with render texture
    private IEnumerator TakeScreenshotAndShare(RenderTexture renderTexture)
    {
        boardCanvas.worldCamera = boardCamera;
        
        UIManager.Instance.HideUI();
        
        yield return new WaitForEndOfFrame();

        var ss = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        ss.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        ss.Apply();
        RenderTexture.active = null;

        var filePath = Path.Combine(Application.temporaryCachePath, "shared img.png");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());

        // To avoid memory leaks
        Destroy(ss);
        
        UIManager.Instance.ShowUI();
        
        boardCanvas.worldCamera = null;

        new NativeShare().SetSubject("Gomoku's Screenshot").SetTitle("Screenshot").AddFile(filePath).Share();
    }
}
