using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI; 
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public bool IsDialogueOpen => dialoguePanel.activeInHierarchy;
    private GameObject currentDialoguePanel;

    [Header("대화 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    
    [SerializeField] private Button actionButton;       
    [SerializeField] private TextMeshProUGUI actionButtonText;
    
    [Header("NPC 기능 패널")]
    public GameObject blacksmithPanel; 
    public GameObject enchantPanel;    
    public GameObject inventoryPanel; 

    [Header("던전 UI")]
    [SerializeField]
    private DungeonSelectPanelController dungeonSelectPanelController;


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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
        if(inventoryPanel != null) inventoryPanel.SetActive(false);
        if(blacksmithPanel != null) blacksmithPanel.SetActive(false);
        if(enchantPanel != null) enchantPanel.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {

    }

    
    void OnEnable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnExitPressed += HandleExitInput;
            InputManager.instance.OnInventoryPressed += ToggleInventoryUI;
        }
    }
    
    void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnExitPressed -= HandleExitInput;
            InputManager.instance.OnInventoryPressed -= ToggleInventoryUI;
        }
    }
    private void HandleExitInput()
    {
        // 1. 대화창이 켜져있으면 -> 닫기
        if (dialoguePanel.activeSelf)
        {
            CloseDialoguePanel();
            return;
        }
        
        if (currentDialoguePanel != null && currentDialoguePanel.activeSelf)
        {
            CloseCurrentPanel();
            return;
        }
        
        
        // 대장간 닫기
        if (blacksmithPanel != null && blacksmithPanel.activeSelf)
        {
            blacksmithPanel.SetActive(false);
            currentDialoguePanel = null; // 상태 초기화
            return;
        }

        // 인챈트 닫기
        if (enchantPanel != null && enchantPanel.activeSelf)
        {
            enchantPanel.SetActive(false);
            currentDialoguePanel = null;
            return;
        }
        
        // 인벤토리 닫기
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(false);
            currentDialoguePanel = null;
            return;
        }
    }
    
    private void ToggleInventoryUI()
    {
        if (IsDialogueOpen) return;
        
        if (inventoryPanel.activeSelf)
        {
            CloseCurrentPanel();
        }
        else
        {
            OpenInventoryUI();
        }
    }
    
    public void OpenDialoguePanel(string speakerName, string dialouge, UnityAction onAction = null, string actionLabel = "")
    {
        speakerNameText.text = speakerName;
        dialogueText.text = dialouge;
        dialoguePanel.SetActive(true);
        
        if (onAction != null)
        {
            actionButton.gameObject.SetActive(true); 
            actionButtonText.text = actionLabel;     
            
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                CloseDialoguePanel(); 
                onAction.Invoke();   
            });
        }
        else
        {
            actionButton.gameObject.SetActive(false); 
        }
    }

    public void CloseDialoguePanel()
    {
        dialoguePanel.SetActive(false);
    }
    
    private void OpenPanel(GameObject panel)
    {
        if (currentDialoguePanel != null)
        {
            currentDialoguePanel.SetActive(false);
        }

        currentDialoguePanel = panel;
        if (currentDialoguePanel != null)
        {
            currentDialoguePanel.SetActive(true);
        }
    }
    
    public void CloseCurrentPanel()
    {
        if (currentDialoguePanel != null)
        {
            currentDialoguePanel.SetActive(false);
            currentDialoguePanel = null;
        }
    }
    
    public void OpenDungeonSelectPanel()
    {
        dungeonSelectPanelController.OpenPanel();
    }
    
    public void OpenBlacksmithUI() => OpenPanel(blacksmithPanel);
    public void OpenEnchantUI() => OpenPanel(enchantPanel);
    public void OpenInventoryUI() => OpenPanel(inventoryPanel);
    
    // 각 UI의 X(닫기) 버튼에 연결할 때는 이 함수들을 쓰거나, CloseCurrentPanel()을 직접 연결해도 됨
    public void CloseBlacksmithUI() => CloseCurrentPanel();
    public void CloseEnchantUI() => CloseCurrentPanel();

}
    

