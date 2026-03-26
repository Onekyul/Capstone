using UnityEngine;

public class SwapWeapon : NPCController
{
    private string npcName = "무기 변경";

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
                () => UIManager.instance.OpenSwapWeaponUI(), "무기 변경");
        }
    }
}
