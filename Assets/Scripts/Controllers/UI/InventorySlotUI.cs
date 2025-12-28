using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{public Image iconImage;
    public TextMeshProUGUI countText; 
    public GameObject emptyIcon; 

    //재료용 
    public void SetMaterial(ItemData data, int count)
    {
        if (data == null) { ClearSlot(); return; }

        ShowIcon(data.icon);
        if (countText != null)
        {
            countText.text = count > 1 ? count.ToString() : "";
        }
    }

    //장비용
    public void SetEquipment(Sprite icon)
    {
        if (icon == null) 
        { 
            ClearSlot(); 
            return; 
        }

        ShowIcon(icon);
        if (countText != null)
        {
            countText.text = "";
        } 
    }

    // 인챈트용
    public void SetEnchant(EnchantData data, int level)
    {
        if (data == null) { ClearSlot(); return; }

        ShowIcon(data.icon);
        if (countText != null)
        {
            countText.text = "";
        }
    }

    //아이콘 켜기
    private void ShowIcon(Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.gameObject.SetActive(true);
        }
        
        if (emptyIcon != null) emptyIcon.SetActive(false);
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        if (iconImage != null) iconImage.gameObject.SetActive(false);
        
        if (countText != null) countText.text = "";
        
        if (emptyIcon != null) emptyIcon.SetActive(true);
    }
}
