using Fusion;
using TMPro;
using UnityEngine;

public class LobbyPlayerStats : NetworkBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text nameText; // 머리 위 닉네임 

    // 네트워크 변수: 닉네임 (값이 바뀌면 자동으로 UpdateUI 실행)
    [Networked, OnChangedRender(nameof(UpdateUI))]
    public NetworkString<_32> Nickname { get; set; }
    
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            string myNickname = "Unknown";
            
            // 우리가 방금 세팅한 DataManager에서 '진짜 닉네임'을 꺼내옵니다!
            if (DataManager.instance != null && DataManager.instance.currentPlayer != null)
            {
                myNickname = DataManager.instance.currentPlayer.nickname;
            }
            RPC_SetNickname(myNickname);
        }
        
        UpdateUI();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(NetworkString<_32> newNickname)
    {
        Nickname = newNickname;
        Debug.Log($"[서버] 플레이어 닉네임 세팅 완료: {newNickname}");
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
