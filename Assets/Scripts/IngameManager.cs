using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class IngameManager : MonoBehaviourPunCallbacks
{
    public static IngameManager Instance { get; private set; }

    public MonsterSpawner TopSpawner;
    public MonsterSpawner DownSpawner;

    public UIGameFailed GameFailed;


    
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

    /// <summary>
    /// 현재 클라이언트의 스포너 인덱스 (1 = 위쪽, 2 = 아래쪽)
    /// 항상 아래쪽(2)이 내 것입니다.
    /// </summary>
    public int MySpawnerIndex { get; private set; } = 2;
    
    /// <summary>
    /// 내 클라이언트가 아래쪽 스포너를 사용하는지 (항상 true)
    /// 에디터에서 확인 가능합니다.
    /// </summary>
    [SerializeField]
    private bool isBottomSpawner = true;

    private bool isGameFailed = false; // 게임 실패 플래그 추가

    public PhotonView PV;

    /// <summary>
    /// 내 클라이언트가 아래쪽 스포너를 사용하는지 (항상 true)
    /// </summary>
    public bool IsBottomSpawner => MySpawnerIndex == 2;
    
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
        if ( PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
        {
            // 방에 2명이 모두 입장했을 때 타이머 시작
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                StartTimer();
                Timer=RoundTimer;
            }
        }

        if(timerStarted&&!isGameFailed)
        {
   // 타이머 실행 중 (게임 시작 전)
            currentTimer -= Time.deltaTime;
            
            // 타이머가 0 이하가 되면 게임 시작
            if (currentTimer <= 0f)
            {
                currentTimer = Timer;
                timerStarted=false;
                StartGame();
            }

            UpdateTimerUI();
        }
        
     
     
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

        
        Debug.Log($"[IngameManager] 라운드 {RoundCount} 시작! 양쪽 스포너 시작");

        UpdateRoundUI();
        
        // 양쪽 스포너 시작 (각 스포너가 자신의 소유권을 확인하여 시작)
        if (TopSpawner != null)
        {
            TopSpawner.RequestStartRound();
        }
        
        if (DownSpawner != null)
        {
            DownSpawner.RequestStartRound();
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
    
    void OnValidate()
    {
        // 에디터에서 자동으로 업데이트
        isBottomSpawner = MySpawnerIndex == 2;
    }
    
    /// <summary>
    /// 몬스터가 현재 클라이언트에 속하는지 확인합니다.
    /// </summary>
    public bool IsMyMonster(Monster monster)
    {
        if (monster == null)
            return false;
            
        // 아래쪽 스포너(인덱스 2)의 몬스터만 내꺼
        return monster.SpanwerIndex == 2;
    }
    
    /// <summary>
    /// 스포너 인덱스로 몬스터가 현재 클라이언트에 속하는지 확인합니다.
    /// </summary>
    public bool IsMyMonster(int spawnerIndex)
    {
        return spawnerIndex == 2;
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
