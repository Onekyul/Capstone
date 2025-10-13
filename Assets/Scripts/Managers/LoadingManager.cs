using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [SerializeField]
    private Slider progressBar; 

    private void Start()
    {
        string targetScene = SceneLoader.Instance.GetNextSceneName();

        if (string.IsNullOrEmpty(targetScene)) {
            SceneManager.LoadScene("MainMenu"); 
            return;
        }

        StartCoroutine(LoadSceneAsync(targetScene));
    }

    private IEnumerator LoadSceneAsync(string sceneName) {
        if (progressBar != null) {
            progressBar.value = 0f;
        }
        
        yield return null; 

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone) {
            yield return null;
            
            timer += Time.deltaTime; 
            float rawProgress = operation.progress;
            float displayProgress = Mathf.Clamp01(rawProgress / 0.9f);

            if (progressBar != null) {
                progressBar.value = Mathf.Lerp(progressBar.value, displayProgress, timer);
            }

            if (rawProgress >= 0.9f && progressBar != null && progressBar.value >= 0.99f)
            {
                operation.allowSceneActivation = true;
                yield break;
            }
        }
    }
}