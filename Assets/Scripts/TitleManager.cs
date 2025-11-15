using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class TitleManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI CreateCodeText;
    public TMP_InputField JoinCodeField;
    public Button CreateCodeButton;
    public Button JoinCodeButton;

    [Header("Scene Settings")]
    [Tooltip("로딩씬 이름 (예: LoadingScene)")]
    public string loadingSceneName = "LoadingScene";

    private int roomCode;
    private bool isConnecting = false;

    public Image FadeImage;

    public TextMeshProUGUI ConnectionText;

    void Start()
    {
        // 버튼 이벤트 연결
        if (CreateCodeButton != null)
        {
            CreateCodeButton.onClick.AddListener(OnCreateRoomButtonClicked);
        }

        if (JoinCodeButton != null)
        {
            JoinCodeButton.onClick.AddListener(OnJoinRoomButtonClicked);
        }

        // 초기 상태 설정
        if (CreateCodeText != null)
        {
            CreateCodeText.text = "코드를 생성하거나 입력하세요";
        }

        // 포톤 연결 상태 확인
        if (!PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
        }

    }

    /// <summary>
    /// 포톤 서버에 연결
    /// </summary>
    void ConnectToPhoton()
    {
        if (isConnecting)
            return;

        isConnecting = true;
        PhotonNetwork.ConnectUsingSettings();
        ConnectionText.text = "서버 연결 시도 중...";
        Debug.Log("포톤 서버 연결 시도 중...");
    }

    /// <summary>
    /// 방 생성 버튼 클릭 시 호출
    /// </summary>
    void OnCreateRoomButtonClicked()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("포톤에 연결되어 있지 않습니다. 연결 중...");
            ConnectToPhoton();
            return;
        }

        // 4자리 랜덤 코드 생성 (1000 ~ 9999)
        roomCode = Random.Range(1000, 10000);
        
        // 방 생성 옵션 설정
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // 2인 게임
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        // 방 이름을 코드로 설정
        string roomName = roomCode.ToString();
        
        // 방 생성
        bool created = PhotonNetwork.CreateRoom(roomName, roomOptions);
        
        if (created)
        {
            Debug.Log($"방 생성 시도: {roomCode}");
            if (CreateCodeText != null)
            {
                CreateCodeText.text = $"코드: {roomCode}";
            }
            CreateCodeButton.interactable = false;
        }
        else
        {
            Debug.LogError("방 생성 실패");
        }
    }

    /// <summary>
    /// 방 조인 버튼 클릭 시 호출
    /// </summary>
    void OnJoinRoomButtonClicked()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("포톤에 연결되어 있지 않습니다. 연결 중...");
            ConnectToPhoton();
            return;
        }

        if (JoinCodeField == null || string.IsNullOrEmpty(JoinCodeField.text))
        {
            Debug.LogWarning("코드를 입력해주세요.");
            return;
        }

        // 입력된 코드로 방 조인 시도
        string inputCode = JoinCodeField.text.Trim();
        
        if (int.TryParse(inputCode, out int code) && code >= 1000 && code <= 9999)
        {
            bool joined = PhotonNetwork.JoinRoom(inputCode);
            
            if (joined)
            {
                Debug.Log($"방 조인 시도: {inputCode}");
                JoinCodeButton.interactable = false;
            }
            else
            {
                Debug.LogError("방 조인 실패");
            }
        }
        else
        {
            Debug.LogWarning("올바른 4자리 코드를 입력해주세요. (1000-9999)");
        }
    }

    #region Photon Callbacks

    /// <summary>
    /// 마스터 서버에 연결 성공 시 호출
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버에 연결되었습니다.");
        isConnecting = false;
        ConnectionText.text = "서버 연결 성공";
        StartCoroutine(Fadein());

    }

    /// <summary>
    /// 방 생성 성공 시 호출
    /// </summary>
    public override void OnCreatedRoom()
    {
        Debug.Log($"방 생성 성공: {PhotonNetwork.CurrentRoom.Name}");
    }

    /// <summary>
    /// 방 입장 성공 시 호출
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"방 입장 성공: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"현재 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

        // 로딩 준비 상태 초기화 (이전 게임의 상태 제거)
        ResetLoadingReadyState();

        // 2명이 모두 입장했으면 로딩씬으로 이동
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("2명이 모두 입장했습니다. 로딩씬으로 이동합니다.");
            LoadLoadingScene();
        }
    }

    /// <summary>
    /// 다른 플레이어가 방에 입장했을 때 호출
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어 입장: {newPlayer.NickName}");
        Debug.Log($"현재 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

        // 로딩 준비 상태 초기화 (이전 게임의 상태 제거)
        ResetLoadingReadyState();

        // 2명이 모두 입장했으면 로딩씬으로 이동
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("2명이 모두 입장했습니다. 로딩씬으로 이동합니다.");
            LoadLoadingScene();
        }
    }

    /// <summary>
    /// 방 생성 실패 시 호출
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message} (코드: {returnCode})");
        CreateCodeButton.interactable = true;
        
        // 같은 코드가 이미 존재하면 다시 시도
        if (returnCode == Photon.Realtime.ErrorCode.GameIdAlreadyExists)
        {
            Debug.Log("이미 존재하는 코드입니다. 새 코드를 생성합니다.");
            OnCreateRoomButtonClicked();
        }
    }

    /// <summary>
    /// 방 조인 실패 시 호출
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 조인 실패: {message} (코드: {returnCode})");
        JoinCodeButton.interactable = true;

        if (returnCode == Photon.Realtime.ErrorCode.GameDoesNotExist)
        {
            Debug.LogWarning("존재하지 않는 방 코드입니다.");
        }
        else if (returnCode == Photon.Realtime.ErrorCode.GameFull)
        {
            Debug.LogWarning("방이 가득 찼습니다.");
        }
    }

    /// <summary>
    /// 연결 실패 시 호출
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"포톤 연결 끊김: {cause}");
        isConnecting = false;
        CreateCodeButton.interactable = true;
        JoinCodeButton.interactable = true;
    }

    #endregion

    /// <summary>
    /// 로딩 준비 상태 초기화
    /// </summary>
    private void ResetLoadingReadyState()
    {
        const string LOADING_READY_KEY = "LoadingReady";
        
        // 로컬 플레이어의 로딩 준비 상태 초기화
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[LOADING_READY_KEY] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        Debug.Log("[TitleManager] 로딩 준비 상태 초기화");
    }

    /// <summary>
    /// 로딩씬으로 이동
    /// </summary>
    void LoadLoadingScene()
    {
        if (!string.IsNullOrEmpty(loadingSceneName))
        {
            // 씬 로드 전에 로딩 준비 상태를 false로 설정 (안전장치)
            ResetLoadingReadyState();
            
            PhotonNetwork.LoadLevel(loadingSceneName);
        }
        else
        {
            Debug.LogError("로딩씬 이름이 설정되지 않았습니다!");
        }
    }

  
    void OnDestroy()
    {
        // 버튼 이벤트 해제
        if (CreateCodeButton != null)
        {
            CreateCodeButton.onClick.RemoveAllListeners();
        }

        if (JoinCodeButton != null)
        {
            JoinCodeButton.onClick.RemoveAllListeners();
        }
    }



     IEnumerator Fadein()
    {
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
        yield return new WaitForSeconds(1f);

    FadeImage.DOFade(0f, 1f).OnComplete(() =>
    {
        FadeImage.gameObject.SetActive(false);
    });
    }
}
