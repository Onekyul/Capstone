using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 파티 대기실 멤버 슬롯 하나.
/// 닉네임, 준비 상태, 방장 여부를 표시한다.
/// </summary>
public class PartyMemberSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private GameObject readyIcon;
    [SerializeField] private GameObject leaderIcon;
    [SerializeField] private GameObject emptyLabel;
    [SerializeField] private Image characterImage;

    public void BindData(PartyMemberDto member, bool isLeader)
    {
        if (emptyLabel != null) emptyLabel.SetActive(false);
        if (characterImage != null) characterImage.gameObject.SetActive(true);
        if (nicknameText != null) nicknameText.text = member.nickname;
        if (leaderIcon != null) leaderIcon.SetActive(isLeader);
        if (readyIcon != null) readyIcon.SetActive(!isLeader && member.isReady);
    }

    public void SetEmpty()
    {
        if (leaderIcon != null) leaderIcon.SetActive(false);
        if (readyIcon != null) readyIcon.SetActive(false);
        if (characterImage != null) characterImage.gameObject.SetActive(false);
        if (nicknameText != null) nicknameText.text = "빈 슬롯";
        if (emptyLabel != null) emptyLabel.SetActive(true);
    }
}
