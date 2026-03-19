using UnityEngine;

public class Alchemist : NPCController
{
    private string npcName = "연금술사";
    [TextArea(3, 10)] public string dialogue;

    [Header("교환 테이블")]
    [SerializeField] private TradeTableData tradeTable;

    public TradeTableData TradeTable => tradeTable;

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
                () => UIManager.instance.OpenAlchemistUI(), "교환");
        }
    }
}
