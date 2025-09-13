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
        dialogueText.text= dialouge;
        dialoguePanel.SetActive(true);
    }

    public void CloseDialoguePanel()
    {
        dialoguePanel.SetActive(false);
    }
}
