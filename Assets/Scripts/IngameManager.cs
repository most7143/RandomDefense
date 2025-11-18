using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;


public class IngameManager : MonoBehaviourPunCallbacks
{
    public static IngameManager Instance { get; private set; }

    [Header("Spawner Settings")]
    [Tooltip("스포너 (하나만 사용)")]
    public MonsterSpawner Spawner;

    [Header("Mirror Settings")]
    [Tooltip("미러링 기준 Y 좌표 (이 좌표를 기준으로 반전)")]
    public float MirrorYOffset = 0f;
    
    [Tooltip("미러링 활성화 여부")]
    public bool EnableMirroring = true;

    public UIGameFailed GameFailed;

    public PlayerController PlayerController;

    public TileGroupController TileGroupController;


    
    [Header("Timer Settings")]
    [Tooltip("시작 타이머 시간 (초)")]
    public float Timer = 5f;

    public float RoundTimer = 20f;

    public int RoundCount = 0;
    public int RoundMaxCount = 10;

    public int MaxMonsterCount = 100;
    
    [Tooltip("타이머를 표시할 UI Text (TextMeshProUGUI 또는 Text)")]
    public TextMeshProUGUI TimerText;

    public TextMeshProUGUI RoundText;

    public TextMeshProUGUI MonsterCountText;
    
    private float currentTimer = 0f;


    private bool timerStarted = false;

    private bool isGameFailed = false; // 게임 실패 플래그 추가

    public PhotonView PV;

    /// <summary>
    /// 현재 클라이언트가 상대방인지 확인 (ActorNumber가 더 큰 플레이어가 상대방, 미러링 적용)
    /// </summary>
    public bool ShouldMirror
    {
        get
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null || PhotonNetwork.PlayerList.Length < 2)
                return false;
            
            // ActorNumber가 더 큰 플레이어가 상대방 (미러링 적용)
            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer && player.ActorNumber > myActorNumber)
                    return true;
            }
            
            return false;
        }
    }
    
    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 타이머 초기화
        currentTimer = Timer;
        UpdateTimerUI();
        UpdateMonsterCountUI(0);
    }
    
      void Update()
    {
        // 게임이 시작되지 않았고 타이머가 시작되지 않았으면 시작
        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
        {
            // 방에 2명이 모두 입장했고, LoadingManager에서 모든 플레이어가 준비되었는지 확인
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                // LoadingManager 확인
                LoadingManager loadingManager = FindObjectOfType<LoadingManager>();
                if (loadingManager == null || IsAllPlayersReady())
                {
                    StartTimer();
                    Timer = RoundTimer;
                }
            }
        }

        if(timerStarted && !isGameFailed)
        {
            // 타이머 실행 중 (게임 시작 전)
            currentTimer -= Time.deltaTime;
            
            // 타이머가 0 이하가 되면 게임 시작
            if (currentTimer <= 0f)
            {
                currentTimer = Timer;
                timerStarted = false;
                StartGame();
            }

            UpdateTimerUI();
        }
    }
    
    /// <summary>
    /// 모든 플레이어가 로딩 준비되었는지 확인
    /// </summary>
    private bool IsAllPlayersReady()
    {
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2)
            return false;
            
        const string LOADING_READY_KEY = "LoadingReady";
        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey(LOADING_READY_KEY))
                return false;
                
            bool playerReady = (bool)player.CustomProperties[LOADING_READY_KEY];
            if (!playerReady)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 타이머 시작
    /// </summary>
    public void StartTimer()
    {
        if (timerStarted)
            return;
            
        timerStarted = true;

        UpdateTimerUI();
        Debug.Log($"[IngameManager] 타이머 시작: {currentTimer}초");
    }
    
    /// <summary>
    /// 게임 시작 (양쪽 스포너 시작)
    /// </summary>
    private void StartGame()
    {
        RoundCount++;

        if (RoundCount >= RoundMaxCount)
        {
            Debug.Log("[IngameManager] 최대 라운드 도달!");
            return;
        }

        
        Debug.Log($"[IngameManager] 라운드 {RoundCount} 시작! 스포너 시작");

        UpdateRoundUI();
        
        // 스포너 시작
        if (Spawner != null)
        {
            Spawner.RequestStartRound();
        }
    }
    
    /// <summary>
    /// 타이머 UI 업데이트
    /// </summary>
    private void UpdateTimerUI()
    {
        int totalSeconds = Mathf.CeilToInt(currentTimer);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        
        // 00:00 형식으로 포맷팅
        string timerString = $"{minutes:D2}:{seconds:D2}";
        
        if (TimerText != null)
        {
            TimerText.text = timerString;
        }
    }
    
    /// <summary>
    /// 라운드 UI 업데이트
    /// </summary>
    private void UpdateRoundUI()
    {
        if (RoundText != null)
        {
            RoundText.text = string.Format("Round {0}", RoundCount);
        }
    }
    
    /// <summary>
    /// 몬스터 카운트 UI 업데이트
    /// </summary>
    public void UpdateMonsterCountUI(int monsterCount)
    {
        if (MonsterCountText != null)
        {
            MonsterCountText.text = string.Format("{0} / {1}", monsterCount, MaxMonsterCount);
        }
    }
    
    /// <summary>
    /// 위치를 미러링합니다 (Y축 기준 반전)
    /// </summary>
    public Vector3 MirrorPosition(Vector3 position)
    {
        if (!EnableMirroring)
            return position;
            
        // Y축 기준으로 반전 (MirrorYOffset 기준)
        float mirroredY = MirrorYOffset - (position.y - MirrorYOffset);
        return new Vector3(position.x, mirroredY, position.z);
    }
    
    /// <summary>
    /// 배열의 위치들을 미러링합니다
    /// </summary>
    public Vector3[] MirrorPositions(Vector3[] positions)
    {
        if (!EnableMirroring || positions == null)
            return positions;
            
        Vector3[] mirrored = new Vector3[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            mirrored[i] = MirrorPosition(positions[i]);
        }
        return mirrored;
    }
    
    /// <summary>
    /// 몬스터가 현재 클라이언트에 속하는지 확인합니다 (PV.IsMine으로 확인)
    /// </summary>
    public bool IsMyMonster(Monster monster)
    {
        if (monster == null || monster.PV == null)
            return false;
            
        return monster.PV.IsMine;
    }

/// <summary>
    /// 게임 실패 처리 (RPC로 모든 클라이언트에 호출)
    /// </summary>
    [PunRPC]
    public void OnGameFailed()
    {
        // 중복 호출 방지
        if (isGameFailed)
            return;
            
        isGameFailed = true;
        
        Debug.Log("[IngameManager] 게임 실패! 모든 클라이언트에 알림");
        
        // 게임 실패 UI 표시
        if (GameFailed != null)
        {
            GameFailed.FadeIn();
        }
        
        // 타이머 정지
        timerStarted = false;
    }
    
    /// <summary>
    /// 게임 실패 요청 (외부에서 호출)
    /// </summary>
    public void RequestGameFailed()
    {
        if (isGameFailed)
            return;
            
        if (PV != null)
        {
            // 모든 클라이언트에 게임 실패 알림
            PV.RPC("OnGameFailed", RpcTarget.All);
        }
        else
        {
            // PhotonView가 없으면 로컬에서만 호출
            OnGameFailed();
        }
    }
}
