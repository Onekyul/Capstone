using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionPanel;

    void Start()
    {
        // 씬 시작 시 OptionPanel을 숨깁니다.
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }
    }
    
    // Main Menu Panel
    public void OnClickStart(){
        Debug.Log("Start");
        SceneLoader.Instance.LoadSceneByButton("BaseArea");
    }

    public void OnClickOption(){
        Debug.Log("Option button clicked. Opening settings.");
        if (mainMenuPanel != null && optionPanel != null)
        {
            mainMenuPanel.SetActive(false);
            optionPanel.SetActive(true);
        }
    }

    public void OnClickExit(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Option Panel
    public void SetVolumeSlider(float value)
    {
        // 실제 오디오 로직은 없고, 슬라이더의 움직임만 콘솔에 표시합니다.
        Debug.Log("Slider Value Changed: " + value.ToString("F2")); 
    }
    
    public void OnClickBackToMain()
    {
        Debug.Log("Back button clicked. Closing settings.");
        if (mainMenuPanel != null && optionPanel != null)
        {
            optionPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}
