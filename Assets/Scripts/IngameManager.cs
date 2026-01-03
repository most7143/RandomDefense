using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


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

    public Round round;

    public UIRound RoundUI;

    private bool isGameFailed = false;

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
        round = new Round();
        Debug.Log(RoundUI);
        round.Timer.OnTick += RoundUI.SetTime;
        round.OnRoundEnded += StartGame;
        round.OnRoundEnded += RoundUI.SetRound;

        Spawner.OnMonsterSpawned += OnMonsterSpawned;

        round.MonsterCounter.OnCountChanged += count =>
        RoundUI.SetMonsterCount(count, round.MonsterCounter.MaxCount);
    }

    void Update()
    {
        // 게임이 시작되지 않았고 타이머가 시작되지 않았으면 시작
        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
        {
            // 방에 2명이 모두 입장했고, LoadingManager에서 모든 플레이어가 준비되었는지 확인
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && IsAllPlayersReady())
            {
                round.Timer.Start();
                round.Tick(Time.deltaTime);
            }
        }
    }

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


    private void StartGame(int roundNumber)
    {

        if (Spawner != null)
        {
            Spawner.RequestStartRound(round);
        }
    }

    private void OnMonsterSpawned(Monster monster)
    {
        round.MonsterCounter.OnMonsterSpawned();

        monster.OnDead += () =>
        {
            round.MonsterCounter.OnMonsterDefeated();
        };
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
