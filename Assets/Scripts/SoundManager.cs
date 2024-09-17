using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource lose, victory, spawn, backgroundMusic, buttonClick;
    [SerializeField] private Sprite bgm, mutedBGM, sfx, mutedSfx;
    [SerializeField] private Slider bgmSlider, sfxSlider;

    private void Start()
    {
        if (!PlayerPrefs.HasKey("bgm"))
        {
            PlayerPrefs.SetFloat("bgm",0.5f);
        }
        if (!PlayerPrefs.HasKey("sfx"))
        {
            PlayerPrefs.SetFloat("sfx",0.5f);
        }
        
        Load();
        ChangeVolBGM();
        ChangeVolSFX();
    }

    private void Load()
    {
        bgmSlider.value = PlayerPrefs.GetFloat("bgm");
        sfxSlider.value = PlayerPrefs.GetFloat("sfx");
    }
    
    private void SaveSFX()
    {
        PlayerPrefs.SetFloat("sfx", sfxSlider.value);
    }

    private void SaveBGM()
    {
        PlayerPrefs.SetFloat("bgm", bgmSlider.value);
    }

    public void ChangeVolBGM()
    {
        backgroundMusic.volume = bgmSlider.value;
        SaveBGM();
        bgmSlider.handleRect.GetComponent<Image>().sprite = bgmSlider.value == 0 ? mutedBGM : bgm;
    }

    public void ChangeVolSFX()
    {
        lose.volume = victory.volume = spawn.volume = sfxSlider.value;
        SaveSFX();
        sfxSlider.handleRect.GetComponent<Image>().sprite = sfxSlider.value == 0 ? mutedSfx : sfx;
    }
    
    public void PlaySpawnSound()
    {
        spawn.PlayOneShot(spawn.clip);
    }
    
    public void PlayLoseSound()
    {
        lose.PlayOneShot(lose.clip);
    }
    
    public void PlayVictorySound()
    {
        victory.PlayOneShot(victory.clip);
    }
    
    public void PlayButtonSound()
    {
        buttonClick.PlayOneShot(buttonClick.clip);
    }
}
