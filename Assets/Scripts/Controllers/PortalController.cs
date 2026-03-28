using UnityEngine;
using UnityEngine.UI;

public class PortalController : MonoBehaviour
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string targetSceneName;

    [Header("확인 팝업")]
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private bool isPlayerInRange = false;

    private void Start()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }

    private void OnEnable()
    {
        if (InputManager.instance != null)
            InputManager.instance.OnInteractPressed += HandleInteraction;
    }

    private void OnDisable()
    {
        if (InputManager.instance != null)
            InputManager.instance.OnInteractPressed -= HandleInteraction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(true);
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            if (confirmPopup != null) confirmPopup.SetActive(false);
            isPlayerInRange = false;
        }
    }

    private void HandleInteraction()
    {
        if (!isPlayerInRange || confirmPopup == null) return;

        confirmButton?.onClick.RemoveAllListeners();
        cancelButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.AddListener(OnConfirm);
        cancelButton?.onClick.AddListener(OnCancel);
        confirmPopup.SetActive(true);
    }

    private void OnConfirm()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (DungeonSessionManager.Instance != null)
            DungeonSessionManager.Instance.EnterFarmingDungeon(targetSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
    }

    private void OnCancel()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }
}
