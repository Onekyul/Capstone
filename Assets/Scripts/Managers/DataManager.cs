using System;
using UnityEngine;
using System.IO;
using System.Text;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public PlayerData currentPlayer;
    private string savePath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        savePath = Path.Combine(Application.persistentDataPath, "save.dat");
        LoadGame();
    }


    //데이터 세이브 및 로드 
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentPlayer);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string code = Convert.ToBase64String(bytes);

        File.WriteAllText(savePath, code);
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string code = File.ReadAllText(savePath);
            byte[] bytes = Convert.FromBase64String(code);
            string json = Encoding.UTF8.GetString(bytes);
            currentPlayer = JsonUtility.FromJson<PlayerData>(json);
        }
        else
        {
            currentPlayer = new PlayerData();
            SaveGame();
        }
    }

    //인벤토리 추가 및 사용
    public void AddInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);

        if (slot != null)
        {
            slot.count += amount; // 이미 있으면 개수 증가
        }
        else
        {
            currentPlayer.Inventory.Add(new PlayerData.InventorySlot(id, amount)); // 없으면 새로 추가
        }
        
        // 데이터가 변했으니 저장
        SaveGame();
    }
    
    public bool UseInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);
        
        if (slot == null || slot.count < amount)
        {
            return false; // 재료 부족
        }

        slot.count -= amount;
        SaveGame();
        return true;
    }
    
    //무기 및 방어구 장착
    public void EquipWeapon(string weaponId)
    {
        if (currentPlayer.ownedWeapons.Exists(w => w.itemId == weaponId))
        {
            currentPlayer.equippedWeaponId = weaponId;
            SaveGame();
        }
    }
    
    public void EquipArmor(string armorId, ArmorType type)
    {
        if (currentPlayer.ownedArmors.Exists(a => a.itemId == armorId))
        {
            switch (type)
            {
                case ArmorType.Helmet: currentPlayer.equippedHelmetId = armorId; break;
                case ArmorType.Armor:  currentPlayer.equippedArmorId = armorId; break;
                case ArmorType.Boots:  currentPlayer.equippedBootsId = armorId; break;
            }
            SaveGame();
        }
    }
    
    
    //SO getter
    public WeaponData GetWeaponData(string id)
    {
        // Resources/Data/Items/Weapons 폴더 안에 있는 파일 로드
        return Resources.Load<WeaponData>($"Data/Items/Weapons/{id}");
    }
    public ArmorData GetArmorData(string id)
    {
        return Resources.Load<ArmorData>($"Data/Items/Armors/{id}");
    }
    public ItemData GetMaterialData(string id)
    {
        return Resources.Load<ItemData>($"Data/Items/Materials/{id}");
    }
    public EnchantData GetEnchantData(string id)
    {
        return Resources.Load<EnchantData>($"Data/Enchants/{id}");
    }

}
