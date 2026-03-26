using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlacksmithUI : MonoBehaviour
{
    public static BlacksmithUI instance;

    [Header("UI 연결")] public Image[] slotImages; // 0:무기, 1:투구, 2:갑옷, 3:신발
    public GameObject[] highlights; // 선택 테두리

    [Header("정보 표시 (현재)")] public TextMeshProUGUI currentNameText;
    public TextMeshProUGUI currentStatText; // "공격력 10"
    public Image currentIcon;

    [Header("정보 표시 (다음 단계)")] public Image nextIcon;
    public TextMeshProUGUI nextNameText;
    public TextMeshProUGUI nextStatText; // "공격력 15"
    public TextMeshProUGUI successRateText;
    public Image materialIcon;
    public TextMeshProUGUI materialText; // "철광석 5 / 10"
    public Button upgradeButton;
    public TextMeshProUGUI materialAmountText;

    private EquipmentType currentType = EquipmentType.Weapon;

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        RefreshAllSlotIcons();
        SelectTab(0);
    }

    // 탭 선택
    public void SelectTab(int typeIndex)
    {
        currentType = (EquipmentType)typeIndex;
        for (int i = 0; i < highlights.Length; i++)
            if (highlights[i] != null)
                highlights[i].SetActive(i == typeIndex);
        UpdateUI();
    }

    public void UpdateUI()
    {
        // 1. 현재 장착된 아이템 정보 가져오기
        string currentId = DataManager.instance.GetEquippedItemId(currentType);
        int currentLevel = DataManager.instance.GetItemLevel(currentId);

        // 변수 준비
        string nameStr = "";
        float currentStat = 0f;
        string statLabel = "";
        Sprite icon = null;
        UpgradeTable table = null;

        WeaponData wData = null;
        ArmorData aData = null;

        // 2. 무기/방어구 구분해서 데이터 로드
        if (currentType == EquipmentType.Weapon)
        {
            wData = DataManager.instance.GetWeaponData(currentId);
            if (wData != null)
            {
                nameStr = wData.weaponName;
                currentStat = wData.GetStat(currentLevel);
                statLabel = "공격력";
                icon = wData.icon;
                table = wData.upgradeTable;
            }
        }
        else
        {
            aData = DataManager.instance.GetArmorData(currentId);
            if (aData != null)
            {
                nameStr = aData.armorName;
                icon = aData.icon;
                table = aData.upgradeTable;

                currentStat = aData.GetStat(currentType, currentLevel);
                switch (currentType)
                {
                    case EquipmentType.Helmet:
                        statLabel = "체력";
                        break;
                    case EquipmentType.Armor:
                        statLabel = "방어력";
                        break;
                    case EquipmentType.Boots:
                        statLabel = "공격속도";
                        break;
                }
            }
        }

        // 현재 정보 표시
        currentNameText.text = $"+{currentLevel} {nameStr}";
        currentStatText.text = $"{statLabel} {currentStat:0.##}";
        if (currentIcon != null) currentIcon.sprite = icon;
        if (slotImages[(int)currentType] != null) slotImages[(int)currentType].sprite = icon;

        //다음 정보
        if (table != null)
        {

            var nextStep = table.GetNextStep(currentLevel);

            if (nextStep != null)
            {
                bool isTierUp = false;

                string nextItemName = nameStr;
                int nextLevelVal = currentLevel + 1;
                Sprite nextIconSprite = icon;
                float nextStatVal = 0f;
              

                if (currentType == EquipmentType.Weapon && nextStep.nextTierWeapon != null)
                {
                    isTierUp = true;
                    nextItemName = nextStep.nextTierWeapon.weaponName;
                    nextLevelVal = 0;
                    nextIconSprite = nextStep.nextTierWeapon.icon;

                    nextStatVal = nextStep.nextTierWeapon.GetStat(0);
                }
                else if (currentType != EquipmentType.Weapon && nextStep.nextTierArmor != null)
                {
                    isTierUp = true;
                    nextItemName = nextStep.nextTierArmor.armorName;
                    nextLevelVal = 0;
                    nextIconSprite = nextStep.nextTierArmor.icon;


                    nextStatVal = nextStep.nextTierArmor.GetStat(currentType, 0);
                }

                else
                {

                    if (currentType == EquipmentType.Weapon && wData != null)
                    {
                        nextStatVal = wData.GetStat(nextLevelVal);
                    }
                    else if (aData != null)
                    {
                        nextStatVal = aData.GetStat(currentType, nextLevelVal);
                    }
                }


                if (nextNameText != null)
                    nextNameText.text = $"+{nextLevelVal} {nextItemName}";

                if (nextIcon != null) nextIcon.sprite = nextIconSprite;

                nextStatText.text = $"{statLabel} {nextStatVal:0.##}";

                successRateText.text = $"{nextStep.successRate}%";

                string matName = nextStep.requiredMaterial != null ? nextStep.requiredMaterial.itemName : "재료";
                string matId = nextStep.requiredMaterial != null ? nextStep.requiredMaterial.itemId : "";
                int matCount = nextStep.materialCount;
                if (nextStep.requiredMaterial != null && materialIcon != null)
                {
                    materialIcon.sprite = nextStep.requiredMaterial.icon;
                    materialIcon.gameObject.SetActive(true); 
                }
                
                int currentCount = DataManager.instance.GetInventoryCount(matId);
                if (materialAmountText != null) 
                    materialAmountText.text = currentCount.ToString();
                materialText.text = $"{matName} ({currentCount}/{matCount})";
                
                bool hasMaterial = currentCount >= matCount;
                materialText.color = hasMaterial ? Color.white : Color.red;
                upgradeButton.interactable = hasMaterial;
                
            }
            else
            {
                if (nextIcon != null) nextIcon.sprite = icon;
                // 다음 단계가 없으면 MAX
                nextStatText.text = "MAX";
                successRateText.text = "-";
                materialText.text = "최고 레벨";
                upgradeButton.interactable = false;
            }
        }
    }

    // 강화 버튼 클릭
    public void OnClickUpgrade()
    {
        string targetId = DataManager.instance.GetEquippedItemId(currentType);
        
        upgradeButton.interactable = false;
        
        Debug.Log("[BlacksmithUI] 서버에 강화 요청 중...");

        // 비동기 콜백 형식으로 변경 (람다식 => 사용)
        DataManager.instance.TryUpgradeItem(targetId, (isSuccess, message) => 
        {
            // -----------------------------------------------------------------
            // 이 중괄호 안의 코드는 서버 통신이 완전히 끝난 후에 실행됩니다!
            // -----------------------------------------------------------------

            if (isSuccess)
            {
                Debug.Log($"[BlacksmithUI] 강화 성공 메시지: {message}");
                
                // 방어구 강화 시 플레이어 스탯 재적용
                if (currentType != EquipmentType.Weapon)
                {
                    if (PlayerStats.Instance != null)
                    {
                        PlayerStats.Instance.UpgradeEquipmentStats();
                        Debug.Log("[BlacksmithUI] 방어구 강화 → 플레이어 스탯 재적용 완료");
                    }
                }
                // 무기 강화 시 무기 데이터 재적용 (던전 씬에서만 WeaponBase 컴포넌트 존재)
                else
                {
                    string equippedWeaponId = DataManager.instance.GetEquippedItemId(EquipmentType.Weapon);

                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        WeaponBase[] weapons = player.GetComponentsInChildren<WeaponBase>(true);
                        foreach (var weapon in weapons)
                        {
                            if (weapon.GetWeaponId() == equippedWeaponId)
                            {
                                weapon.UpgradeWeaponDamage();
                                Debug.Log($"[BlacksmithUI] 장착된 무기({equippedWeaponId}) 강화 완료!");
                            }
                        }
                    }
                    // 로비에서는 WeaponBase 컴포넌트가 없으므로 저장된 데이터 갱신으로 충분
                }
            }
            else
            {
                // 실패 (확률 실패, 혹은 재료 부족)
                Debug.Log($"[BlacksmithUI] 강화 실패: {message}");
            }
            
            UpdateUI(); 
            
        });
    }

    public void RefreshAllSlotIcons()
    {
        for (int i = 0; i < 4; i++)
        {
            if (slotImages[i] == null) continue;

            EquipmentType type = (EquipmentType)i;
            string id = DataManager.instance.GetEquippedItemId(type);
            
            Sprite icon = GetItemIcon(type, id);
            if (icon != null) slotImages[i].sprite = icon;
        }
    }
    
    Sprite GetItemIcon(EquipmentType type, string id)
    {
        if (type == EquipmentType.Weapon)
        {
            var data = DataManager.instance.GetWeaponData(id);
            return data != null ? data.icon : null;
        }
        else
        {
            var data = DataManager.instance.GetArmorData(id);
            return data != null ? data.icon : null;
        }
    }

}