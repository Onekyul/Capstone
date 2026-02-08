using Fusion;
using TMPro;
using UnityEngine;

public class LobbyPlayerStats : NetworkBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text nameText; // 머리 위 닉네임 

    // 네트워크 변수: 닉네임 (값이 바뀌면 자동으로 UpdateUI 실행)
    [Networked, OnChangedRender(nameof(UpdateUI))]
    public NetworkString<_16> Nickname { get; set; }
    
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            int myId = (DataManager.instance != null) ? DataManager.instance.MyUserId : 0;
            
            Nickname = $"User {myId}";
        }
        
        UpdateUI();
    }

    
    void UpdateUI()
    {
        if (nameText == null) return;
        
        nameText.text = Nickname.ToString();
        
        if (HasStateAuthority)
        {
            nameText.color = Color.green; 
        }
        else
        {
            nameText.color = Color.white;
        }
    }
}
