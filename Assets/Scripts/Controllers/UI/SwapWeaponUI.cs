using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 무기 타입 전환 UI.
/// 3종 무기(검/창/활) 모두 보유 상태에서 타입 전환.
/// EquipWeapon() + SaveGame()으로 서버 동기화.
/// </summary>
public class SwapWeaponUI : MonoBehaviour
{
    [Header("무기 슬롯 (검/창/활)")]
    [SerializeField] private Button swordButton;
    [SerializeField] private Button spearButton;
    [SerializeField] private Button bowButton;

    [SerializeField] private Image swordIcon;
    [SerializeField] private Image spearIcon;
    [SerializeField] private Image bowIcon;

    [SerializeField] private TextMeshProUGUI swordNameText;
    [SerializeField] private TextMeshProUGUI spearNameText;
    [SerializeField] private TextMeshProUGUI bowNameText;

    [Header("장착 표시")]
    [SerializeField] private GameObject swordEquippedMark;
    [SerializeField] private GameObject spearEquippedMark;
    [SerializeField] private GameObject bowEquippedMark;

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (DataManager.instance == null) return;

        int currentType = DataManager.instance.GetWeaponTypeFromId(
            DataManager.instance.currentPlayer.equippedWeaponId);

        SetupSlot(0, swordButton, swordIcon, swordNameText, swordEquippedMark, currentType);
        SetupSlot(1, spearButton, spearIcon, spearNameText, spearEquippedMark, currentType);
        SetupSlot(2, bowButton, bowIcon, bowNameText, bowEquippedMark, currentType);
    }

    private void SetupSlot(int weaponType, Button button, Image icon, TextMeshProUGUI nameText, GameObject equippedMark, int currentType)
    {
        string weaponId = DataManager.instance.GetEquippedWeaponId(weaponType);
        WeaponData data = null;

        if (!string.IsNullOrEmpty(weaponId))
            data = DataManager.instance.GetWeaponData(weaponId);

        if (data != null)
        {
            if (icon != null) { icon.sprite = data.icon; icon.enabled = true; }
            if (nameText != null)
            {
                int level = DataManager.instance.GetItemLevel(weaponId);
                nameText.text = level > 0 ? $"{data.weaponName} +{level}" : data.weaponName;
            }

            button.interactable = (weaponType != currentType);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnWeaponSelected(weaponId));
        }
        else
        {
            if (icon != null) icon.enabled = false;
            if (nameText != null) nameText.text = "미보유";
            button.interactable = false;
        }

        if (equippedMark != null)
            equippedMark.SetActive(weaponType == currentType);
    }

    private void OnWeaponSelected(string weaponId)
    {
        DataManager.instance.EquipWeapon(weaponId); // 로컬 적용 + SaveGame
        RefreshUI();
    }
}
