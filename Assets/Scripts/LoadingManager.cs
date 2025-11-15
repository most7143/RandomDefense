using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    private const string LOADING_READY_KEY = "LoadingReady";
    
    private bool isLocalReady = false;
    private bool allPlayersReady = false;
    
    [Header("Scene Settings")]
    [Tooltip("메인씬 이름 (예: MainScene)")]
    public string mainSceneName = "MainScene";
    
    [Header("UI References")]
    [Tooltip("로딩 상태를 표시할 텍스트 (선택사항)")]
    public TMPro.TextMeshProUGUI LoadingText;
    
    void Start()
    {
        // Photon 연결 확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[LoadingManager] Photon에 연결되어 있지 않습니다.");
            return;
        }
        
        // 초기 로딩 상태 표시
        UpdateLoadingText("씬 로딩 중...");
        
        // 씬이 완전히 로드될 때까지 대기
        StartCoroutine(WaitForSceneLoad());
    }
    
    /// <summary>
    /// 씬이 완전히 로드될 때까지 대기
    /// </summary>
    private IEnumerator WaitForSceneLoad()
    {
        // 씬이 완전히 로드될 때까지 대기
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
        yield return new WaitForSeconds(0.5f); // 추가 안정화 시간
        
        // 모든 오브젝트가 초기화될 때까지 대기
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);
        
        // 로컬 클라이언트 준비 완료
        SetLocalReady();
    }
    
    /// <summary>
    /// 로컬 클라이언트 준비 완료 표시
    /// </summary>
    private void SetLocalReady()
    {
        if (isLocalReady)
            return;
            
        isLocalReady = true;
        
        // Custom Properties에 준비 완료 플래그 설정
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[LOADING_READY_KEY] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        Debug.Log("[LoadingManager] 로컬 클라이언트 로딩 완료");
        UpdateLoadingText("로딩 완료. 다른 플레이어 대기 중...");
        
        // 다른 플레이어들의 상태 확인
        CheckAllPlayersReady();
    }
    
    /// <summary>
    /// 모든 플레이어가 준비되었는지 확인
    /// </summary>
    private void CheckAllPlayersReady()
    {
        if (allPlayersReady)
            return;
            
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.LogWarning("[LoadingManager] 방에 플레이어가 2명 미만입니다.");
            return;
        }
        
        // 모든 플레이어가 준비되었는지 확인
        bool everyoneReady = true;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey(LOADING_READY_KEY))
            {
                bool playerReady = (bool)player.CustomProperties[LOADING_READY_KEY];
                if (!playerReady)
                {
                    everyoneReady = false;
                    break;
                }
            }
            else
            {
                everyoneReady = false;
                break;
            }
        }
        
        if (everyoneReady && !allPlayersReady)
        {
            allPlayersReady = true;
            OnAllPlayersReady();
        }
    }
    
    /// <summary>
    /// 모든 플레이어가 준비되었을 때 호출
    /// </summary>
    private void OnAllPlayersReady()
    {
        Debug.Log("[LoadingManager] 모든 플레이어 로딩 완료! 메인씬으로 이동합니다.");
        UpdateLoadingText("모든 플레이어 준비 완료! 메인씬으로 이동 중...");
        
        // 잠시 대기 후 메인씬으로 이동
        StartCoroutine(LoadMainSceneAfterDelay());
    }
    
    /// <summary>
    /// 잠시 대기 후 메인씬으로 이동
    /// </summary>
    private IEnumerator LoadMainSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // 짧은 대기 시간
        
        if (!string.IsNullOrEmpty(mainSceneName))
        {
            PhotonNetwork.LoadLevel(mainSceneName);
        }
        else
        {
            Debug.LogError("[LoadingManager] 메인씬 이름이 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 로딩 텍스트 업데이트
    /// </summary>
    private void UpdateLoadingText(string text)
    {
        if (LoadingText != null)
        {
            LoadingText.text = text;
        }
        Debug.Log($"[LoadingManager] {text}");
    }
    
    #region Photon Callbacks
    
    /// <summary>
    /// 플레이어의 Custom Properties가 변경되었을 때 호출
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(LOADING_READY_KEY))
        {
            Debug.Log($"[LoadingManager] 플레이어 {targetPlayer.NickName}의 로딩 상태 업데이트");
            CheckAllPlayersReady();
        }
    }
    
    /// <summary>
    /// 다른 플레이어가 방에 입장했을 때 호출
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[LoadingManager] 플레이어 {newPlayer.NickName} 입장");
        CheckAllPlayersReady();
    }
    
    /// <summary>
    /// 플레이어가 방을 떠났을 때 호출
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[LoadingManager] 플레이어 {otherPlayer.NickName} 퇴장");
        // 플레이어가 나가면 준비 상태 초기화
        allPlayersReady = false;
    }
    
    #endregion
    
    void Update()
    {
        // 매 프레임 확인 (백업용, OnPlayerPropertiesUpdate가 호출되지 않는 경우 대비)
        if (isLocalReady && !allPlayersReady)
        {
            CheckAllPlayersReady();
        }
    }
}