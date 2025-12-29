using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionPanel;

    void Start()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }
    }
    
    // Main Menu Panel
    public void OnClickStart(){
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
        Debug.Log("Slider Value Changed: " + value.ToString("F2")); 
    }
    
    public void OnClickBackToMain()
    {
        if (mainMenuPanel != null && optionPanel != null)
        {
            optionPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}
