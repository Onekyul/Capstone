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
            UIManager.instance.OpenDialoguePanel(npcName, dialogue);
        }
    }
    
    public void UpgradeItem(EquipmentType targetType)
    {
        string currentItemId = DataManager.instance.GetEquippedItemId(targetType);
        
        if (string.IsNullOrEmpty(currentItemId))
        {
            Debug.Log("장착된 아이템이 없습니다.");
            return;
        }

        
        int currentLevel = DataManager.instance.GetItemLevel(currentItemId);

        //레시피 선언
        UpgradeTable upgradeTable = null;
        string itemName = "";

        if (targetType == EquipmentType.Weapon)
        {
            WeaponData wData = DataManager.instance.GetWeaponData(currentItemId);
            if (wData != null)
            {
                upgradeTable = wData.upgradeTable;
                itemName = wData.weaponName;
            }
        }
        else 
        {
            ArmorData aData = DataManager.instance.GetArmorData(currentItemId);
            if (aData != null)
            {
                upgradeTable = aData.upgradeTable;
                itemName = aData.armorName;
            }
        }

        //레시피 검사
        if (upgradeTable == null)
        {
            Debug.Log($"{itemName}은(는) 강화할 수 없는 아이템입니다.");
            return;
        }

        var nextStep = upgradeTable.GetNextStep(currentLevel);
        if (nextStep == null)
        {
            Debug.Log("이미 최고 레벨입니다.");
            return;
        }

        //재료 검사
        string matId = nextStep.requiredMaterial.itemId;
        int matCount = nextStep.materialCount;

        if (!DataManager.instance.HasInventory(matId, matCount))
        {
            Debug.Log($"재료 부족 ({nextStep.requiredMaterial.itemName} {matCount}개 필요)");
            return;
        }

        // 강화 실행
        DataManager.instance.UseInventory(matId, matCount);

        int randomVal = Random.Range(0, 100);
        if (randomVal < nextStep.successRate)
        {
            DataManager.instance.UpgradeEquipment(currentItemId);
            Debug.Log($"[강화 성공] {itemName} (+{currentLevel + 1})");
            // TODO: 성공 UI 갱신, 이펙트 재생
        }
        else
        {
            // 실패!
            Debug.Log($"[강화 실패] {itemName}...");
            // TODO: 실패 UI 갱신, 펑 터지는 이펙트
        }
    }
    
}
