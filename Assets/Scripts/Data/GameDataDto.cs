using System;
using System.Collections.Generic;

//DTO를 명시
[Serializable]
public class GameDataDto
{
    public int userId;
    public string nickname;
    public int stage;
    public EquipDto equip;          
    
  
    public List<ItemDto> inventory = new List<ItemDto>();       
    public List<EquipItemDto> equipments = new List<EquipItemDto>();
    public List<EnchantDto> enchants = new List<EnchantDto>();
}

[Serializable]
public class EquipDto 
{
    public string weapon;
    public string helmet;
    public string armor;
    public string boots;
}

[Serializable]
public class ItemDto 
{
    public string id;
    public int count;
    public ItemDto(string id, int count) { this.id = id; this.count = count; }
}

[Serializable]
public class EquipItemDto 
{
    public string id;
    public int level;
    public EquipItemDto(string id, int level) { this.id = id; this.level = level; }
}

[Serializable]
public class EnchantDto 
{
    public string id;
    public int level;
    public EnchantDto(string id, int level) { this.id = id; this.level = level; }
}

[Serializable]
public class LoginResponseDto
{
    public int userId;    // 내 유저 ID (DB의 Primary Key)
    public string nickname; // 닉네임
    public int stage;     // 현재 스테이지 (서버가 보내준다면)
}