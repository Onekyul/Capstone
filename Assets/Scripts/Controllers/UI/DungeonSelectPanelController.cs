using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    
    // 이제 이 스크립트가 자신의 버튼들을 직접 관리
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
            SceneManager.LoadScene("FireDungeonScene1 1");

        }
    }

    private void ResetSelection()
    {
        selectedFloor = 0;
        floor1Button.image.color = Color.white;
        enterButton.interactable = false;
    }
}
