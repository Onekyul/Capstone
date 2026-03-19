using UnityEngine;
using UnityEngine.UI;

public class DungeonSelectPanelController : MonoBehaviour
{
    [SerializeField] private Button floor1Button;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button closeButton;

    private int selectedFloor = 0;

    // 패널이 활성화될 때 초기화
    private void OnEnable()
    {
        ResetSelection();
    }

    // 이 패널을 열고 닫는 public 함수
    public void OpenPanel()
    {
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    public void SelectFloor(int floorNumber)
    {
        selectedFloor = floorNumber;
        floor1Button.image.color = Color.yellow;
        enterButton.interactable = true;

        Debug.Log("SElectd");
    }

    public void EnterDungeon()
    {
        if (selectedFloor > 0)
        {
            ClosePanel();
            if (DungeonSessionManager.Instance != null)
            {
                // 씬 전환 코루틴은 DontDestroyOnLoad인 DungeonSessionManager에서 실행
                DungeonSessionManager.Instance.EnterFarmingDungeon("FireDungeonScene1");
            }
            else
            {
                // DungeonSessionManager가 없으면 직접 로드 (Fusion 미사용 환경)
                Debug.LogWarning("[DungeonSelect] DungeonSessionManager 없음 - SceneManager 직접 사용");
                UnityEngine.SceneManagement.SceneManager.LoadScene("FireDungeonScene1");
            }
        }
    }

    private void ResetSelection()
    {
        selectedFloor = 0;
        floor1Button.image.color = Color.white;
        enterButton.interactable = false;
    }
}
