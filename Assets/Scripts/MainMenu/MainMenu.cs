using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System;
public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionPanel;

    public GameObject nicknamePanel; 
    public TMP_Text panelText;
    public TMP_InputField nicknameInputField;
    
    public Button checkDuplicateButton; // 중복 확인 버튼
    public Button submitButton;
    
    private const string DEVICE_ID_KEY = "Capstone_DeviceID";
    
    private string validatedNickname = "";
    void Start()
    {
        if (optionPanel != null) optionPanel.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);

        // 시작 시 최종 가입 버튼은 비활성화 상태로 둡니다.
        if (submitButton != null) submitButton.interactable = false;

        // 인풋필드 글자가 바뀔 때마다 감지하는 이벤트 연결
        nicknameInputField.onValueChanged.AddListener(OnNicknameInputChanged);
    }
    
    // Main Menu Panel
    public void OnClickStart()
    {
        string savedDeviceId = PlayerPrefs.GetString(DEVICE_ID_KEY, "");

        if (string.IsNullOrEmpty(savedDeviceId))
        {
            // [신규 유저] 환영 문구 띄우고 닉네임 패널 열기
            if (panelText != null) panelText.text = "환영합니다!\n사용하실 닉네임을 입력해주세요.";
            
            nicknamePanel.SetActive(true);
        }
        else
        {
            // [기존 유저] 로그인 + 데이터 로드 후 로비 이동
            if (DataManager.instance != null)
            {
                DataManager.instance.InitializeNetwork(
                    () =>
                    {
                        SceneLoader.Instance.LoadSceneByButton("BaseArea 1");
                    },
                    () =>
                    {
                        // 로그인 실패 (EC2에 없는 deviceId 등) → 닉네임 패널 열기
                        PlayerPrefs.DeleteKey(DEVICE_ID_KEY);
                        PlayerPrefs.Save();
                        if (panelText != null) panelText.text = "환영합니다!\n사용하실 닉네임을 입력해주세요.";
                        nicknamePanel.SetActive(true);
                    }
                );
            }
        }
    }

   private void OnNicknameInputChanged(string text)
    {
        // 중복 확인을 통과했던 닉네임과 현재 텍스트가 다르면 가입 버튼을 다시 잠금
        if (text != validatedNickname)
        {
            submitButton.interactable = false;
        }
    }

  
    public void OnClickCheckDuplicate()
    {
        string inputNickname = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(inputNickname) || inputNickname.Length < 2)
        {
            Debug.LogWarning("[MainMenu] 닉네임은 2글자 이상 입력해야 합니다!");
            return;
        }

        // SessionManager를 통해 서버에 사용 가능 여부 묻기
        SessionManager.Instance.CheckNicknameDuplicate(inputNickname, (isAvailable) =>
        {
            if (isAvailable)
            {
                Debug.Log("[MainMenu] 사용 가능한 닉네임입니다!");
                validatedNickname = inputNickname;     // 검증된 닉네임 저장
                submitButton.interactable = true;      // ★ 확인(가입) 버튼 활성화!
            }
            else
            {
                Debug.LogWarning("[MainMenu] 이미 사용 중인 닉네임입니다. 다른 닉네임을 입력하세요.");
                submitButton.interactable = false;     // 확인 버튼 비활성화 유지
            }
        });
    }
    
    public void OnClickSubmitNickname()
    {
        // 최종적으로 한 번 더 검증
        if (nicknameInputField.text.Trim() != validatedNickname) return;

        string newDeviceId = Guid.NewGuid().ToString();
        submitButton.interactable = false; // 중복 터치 방지

        SessionManager.Instance.RegisterNewUser(newDeviceId, validatedNickname, (isSuccess) =>
        {
            if (isSuccess)
            {
                PlayerPrefs.SetString(DEVICE_ID_KEY, newDeviceId);
                PlayerPrefs.Save();

                if (DataManager.instance != null)
                {
                    Debug.Log("[MainMenu] 초기 게임 데이터(나무 장비 등)를 서버에 Save 요청합니다.");
                    DataManager.instance.SaveGame(); 

                    SceneLoader.Instance.LoadSceneByButton("BaseArea 1");
                }
            }
            else
            {
                Debug.LogWarning("[MainMenu] 회원가입 실패. 서버 상태를 확인해주세요.");
                submitButton.interactable = true;
            }
        });
    }
    
    
    public void OnClickOption(){
        Debug.Log("Option button clicked. Opening settings.");
        if (mainMenuPanel != null && optionPanel != null)
        {
            mainMenuPanel.SetActive(false);
            optionPanel.SetActive(true);
        }
    }

    public void OnClickExit(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Option Panel
    public void SetVolumeSlider(float value)
    {
        Debug.Log("Slider Value Changed: " + value.ToString("F2")); 
    }
    
    public void OnClickBackToMain()
    {
        if (mainMenuPanel != null && optionPanel != null)
        {
            nicknamePanel.SetActive(false);
            nicknameInputField.text = "";
            validatedNickname = "";
            optionPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}
