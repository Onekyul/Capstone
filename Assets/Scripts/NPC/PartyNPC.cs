using UnityEngine;

public class PartyNPC : NPCController
{
    private string npcName = "게시판";

    [TextArea(3, 10)] public string dialogue;

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
            UIManager.instance.OpenDialoguePanel(npcName, dialogue,
                () => UIManager.instance.OpenPartyUI(), "파티 목록");
        }
    }
}
