using UnityEngine;

public class BlackSmith : NPCController
{
    private string npcName = "대장장이";
    [TextArea(3, 10)] public string dialogue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    

    public override void Interact()
    {
        if (UIManager.instance.IsDialogueOpen)
        {
            UIManager.instance.CloseDialoguePanel();
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
        else
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            UIManager.instance.OpenDialoguePanel(npcName,dialogue,
                () => UIManager.instance.OpenBlacksmithUI(),"강화");
        }
    }
    
   
    
}
