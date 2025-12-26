using UnityEngine;

public class Wizard : NPCController
{
    private string npcName = "마법사";

    [TextArea(3, 10)] public string dialogue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  

    // Update is called once per frame
    void Update()
    {
        
    }
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
                () => UIManager.instance.OpenEnchantUI(), "인챈트");
        }
    }
    
    public void TryUpgradeCurrentEnchant(string id)
    {
        
        EnchantData currentSelectedEnchant=DataManager.instance.GetEnchantData(id);
        
        if (currentSelectedEnchant == null)
        {
            Debug.Log("강화할 속성이 선택되지 않았습니다.");
            return;
        }

        string enchantId = currentSelectedEnchant.enchantId;

       
        int currentLevel = DataManager.instance.GetEnchantLevel(enchantId);

       
        var nextInfo = currentSelectedEnchant.GetNextLevelInfo(currentLevel);

        if (nextInfo == null)
        {
            Debug.Log("이미 최고 레벨입니다.");
            return;
        }

        // 재료 검사
        foreach (var req in nextInfo.requiredMaterials)
        {
            
            if (!DataManager.instance.HasInventory(req.material.itemId, req.count))
            {
                Debug.Log($"재료 부족: {req.material.itemName} ({req.count}개 필요)");
                return;
            }
        }

        //  강화
        foreach (var req in nextInfo.requiredMaterials)
        {
            DataManager.instance.UseInventory(req.material.itemId, req.count);
        }
        
        int randomVal = Random.Range(0, 100);
        
        if (randomVal < nextInfo.successRate)
        {
            
            DataManager.instance.UpgradeEnchant(enchantId);
            Debug.Log($" [인챈트 성공] ({currentSelectedEnchant.enchantName} +{currentLevel + 1})");
            
            // 플레이어의 무기에 즉시 반영
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                WeaponBase[] weapons = player.GetComponentsInChildren<WeaponBase>(true);
                foreach (var weapon in weapons)
                {
                    weapon.UpgradeEnchantLevels();
                }
            }
           
            // SelectEnchant(currentSelectedEnchant); 
        }
        else
        {
            Debug.Log("[인챈트 실패]");
        }
    }
}
