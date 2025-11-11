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
    
    [Header("Timer Settings")]
    [Tooltip("시작 타이머 시간 (초)")]
    public float Timer = 5f;
    
    [Tooltip("타이머를 표시할 UI Text (TextMeshProUGUI 또는 Text)")]
    public TextMeshProUGUI TimerText;
    
    private float currentTimer = 0f;
    private bool timerStarted = false;
    private bool gameStarted = false;
    
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
    
    /// <summary>
    /// 내 클라이언트가 아래쪽 스포너를 사용하는지 (항상 true)
    /// </summary>
    public bool IsBottomSpawner => MySpawnerIndex == 2;
    
    /// <summary>
    /// 게임이 시작되었는지 확인
    /// </summary>
    public bool IsGameStarted => gameStarted;

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
    }
    
    void Update()
    {
        // 게임이 시작되지 않았고 타이머가 시작되지 않았으면 시작
        if (!gameStarted && !timerStarted && PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
        {
            // 방에 2명이 모두 입장했을 때 타이머 시작
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                StartTimer();
            }
        }
        
        // 타이머 실행 중
        if (timerStarted && !gameStarted)
        {
            currentTimer -= Time.deltaTime;
            
            // 타이머가 0 이하가 되면 게임 시작
            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
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
        currentTimer = Timer;
        UpdateTimerUI();
        Debug.Log($"[IngameManager] 타이머 시작: {Timer}초");
    }
    
    /// <summary>
    /// 게임 시작 (양쪽 스포너 시작)
    /// </summary>
    private void StartGame()
    {
        if (gameStarted)
            return;
            
        gameStarted = true;
        timerStarted = false;
        
        Debug.Log("[IngameManager] 게임 시작! 양쪽 스포너 시작");
        
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
    
    public override void OnJoinedRoom()
    {
        // 방에 입장하면 타이머 시작 체크
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            StartTimer();
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 다른 플레이어가 입장하면 타이머 시작
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && !timerStarted)
        {
            StartTimer();
        }
    }
}
