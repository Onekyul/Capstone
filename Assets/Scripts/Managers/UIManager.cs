using UnityEngine;
using TMPro;
using System.Collections.Generic;
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

}
    

