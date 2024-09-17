using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject playMode;
    [SerializeField] private GameObject multiMode;
    [SerializeField] private GameObject level;

    private AudioSource _selectAudio;
    
    private void Start()
    {
        _selectAudio = GetComponent<AudioSource>();
    }

    public void SinglePlay()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        playMode.SetActive(false);
        level.SetActive(true);
        GameManager.Instance.GameMode = "Single";
    }

    public void MultiPlayer()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        playMode.SetActive(false);
        multiMode.SetActive(true);
    }

    public void Local()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        GameManager.Instance.GameMode = "Local";
        SceneManager.LoadScene("Game");
    }

    public void Online()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        GameManager.Instance.GameMode = "Online";
        SceneManager.LoadScene("Game");
        ClientManager.Instance.ConnectToServer();
    }

    public void EasyMode()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        SceneManager.LoadScene("Game");
        GameManager.Instance.GameLevel = "Easy";
    }
    
    public void NormalMode()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        SceneManager.LoadScene("Game");
        GameManager.Instance.GameLevel = "Normal";
    }
    
    public void HardMode()
    {
        _selectAudio.PlayOneShot(_selectAudio.clip);
        SceneManager.LoadScene("Game");
        GameManager.Instance.GameLevel = "Hard";
    }
}
