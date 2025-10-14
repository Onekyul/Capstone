using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; } 

    private string nextSceneName; 

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        } 
        else 
        {
            Destroy(gameObject);
        }
    }

    public void LoadSceneByButton(string targetSceneName) 
    {
        nextSceneName = targetSceneName; 
        SceneManager.LoadScene("LoadingScene");
    }

    public string GetNextSceneName()
    {
        return nextSceneName;
    }
}