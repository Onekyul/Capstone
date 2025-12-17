using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public bool IsDialogueOpen => dialoguePanel.activeInHierarchy;

    [Header("대화 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("던전 UI")]
    [SerializeField]
    private DungeonSelectPanelController dungeonSelectPanelController;
    [Tooltip("레벨업 시 켜질 전체 패널 (배경 포함)")]
    public GameObject levelUpPanel;

    [Tooltip("능력 버튼들이 생성될 부모 오브젝트 (Horizontal Layout Group 추천)")]
    public Transform abilityContainer;

    [Tooltip("생성할 능력 버튼의 프리팹")]
    public GameObject abilityButtonPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        //시작할 때는 레벨업 패널 꺼두기
        levelUpPanel.SetActive(false);
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {

    }

    public void OpenDialoguePanel(string speakerName, string dialouge)
    {
        speakerNameText.text = speakerName;
        dialogueText.text = dialouge;
        dialoguePanel.SetActive(true);
    }

    public void CloseDialoguePanel()
    {
        dialoguePanel.SetActive(false);
    }
    public void OpenDungeonSelectPanel()
    {
        dungeonSelectPanelController.OpenPanel();
    }
    // LevelManager가 호출하는 함수: 화면을 띄우고 버튼을 만듦
    public void ShowLevelUpScreen(List<AbilityData> options)
    {
        levelUpPanel.SetActive(true);

        // 1. 기존에 떠 있던 버튼들이 있다면 싹 지워줍니다. (초기화)
        foreach (Transform child in abilityContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 받아온 옵션 개수만큼 버튼을 새로 만듭니다.
        foreach (AbilityData ability in options)
        {
            GameObject newBtn = Instantiate(abilityButtonPrefab, abilityContainer);

            // 버튼에 데이터 심어주기 (이 스크립트는 바로 아래 3번에서 만듭니다)
            AbilityButton buttonScript = newBtn.GetComponent<AbilityButton>();
            buttonScript.Setup(ability);
        }
    }

    // LevelManager가 호출하는 함수: 화면 끄기
    public void HideLevelUpScreen()
    {
        levelUpPanel.SetActive(false);
    }
}
    

