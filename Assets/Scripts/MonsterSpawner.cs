using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class MonsterSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawner Settings")]
    [Tooltip("스포너 인덱스 (Y 위치로 자동 할당됩니다)")]
    public int SpawnerIndex = 0;
    
    [Header("Monster Settings")]
    [Tooltip("스폰할 몬스터 타입 (Resources 폴더에서 프리팹을 로드합니다)")]
    public MonsterNames MonsterType = MonsterNames.Bird1;
    
    [Tooltip("라운드별 몬스터 수 (인덱스 0 = 라운드 1)")]
    public int[] monstersPerRound = new int[] { 40 };
    
    [Header("Spawn Timing")]
    [Tooltip("몬스터 간 스폰 간격 (초)")]
    public float spawnInterval = 0.5f;
    
    
    private int currentRound = 1;
    private bool isSpawning = false;
    private int monstersSpawnedThisRound = 0;

    [Header("Move Points")]
    [Tooltip("몬스터 이동 경로 포인트들")]
    public Transform[] MovePoints;
    
    private PhotonView pv;
    private bool isMySpawner = false;
    
    void Start()
    {
        // PhotonView 컴포넌트 가져오기
        pv = GetComponent<PhotonView>();
        
        // PhotonView가 없으면 추가
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
            pv.ViewID = PhotonNetwork.AllocateViewID(0);
        }
        
        // Y 위치로 스포너 인덱스 자동 설정
        SetSpawnerIndexByPosition();
        
        // IngameManager에 스포너 등록
        RegisterSpawnerToManager();
        
        // 스포너 소유권 확인
        CheckSpawnerOwnership();
        
    }
    
    /// <summary>
    /// IngameManager에 스포너 등록
    /// </summary>
    private void RegisterSpawnerToManager()
    {
        if (IngameManager.Instance == null)
            return;
        
        if (SpawnerIndex == 1)
        {
            IngameManager.Instance.TopSpawner = this;
        }
        else if (SpawnerIndex == 2)
        {
            IngameManager.Instance.DownSpawner = this;
        }
    }
    
    /// <summary>
    /// Y 위치로 스포너 인덱스 설정 (Y가 높으면 위쪽=1, 낮으면 아래쪽=2)
    /// </summary>
    private void SetSpawnerIndexByPosition()
    {
        MonsterSpawner[] allSpawners ={IngameManager.Instance.TopSpawner,IngameManager.Instance.DownSpawner};
        if (allSpawners.Length >= 2)
        {
            // Y 위치로 정렬 (높은 Y가 위쪽)
            System.Array.Sort(allSpawners, (a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
            
            // 이 스포너가 위쪽인지 아래쪽인지 확인
            if (allSpawners[0] == this)
                SpawnerIndex = 1; // 위쪽
            else if (allSpawners[1] == this)
                SpawnerIndex = 2; // 아래쪽
        }
    }
    
    /// <summary>
    /// 이 스포너가 현재 클라이언트에 속하는지 확인합니다.
    /// </summary>
    private void CheckSpawnerOwnership()
    {
        if (IngameManager.Instance == null || SpawnerIndex == 0)
        {
            isMySpawner = false;
            return;
        }
        
        // IngameManager의 MySpawnerIndex와 비교
        isMySpawner = (SpawnerIndex == IngameManager.Instance.MySpawnerIndex);
        
        Debug.Log($"[스포너 {SpawnerIndex}] 내 스포너 여부: {isMySpawner} (IngameManager.MySpawnerIndex: {IngameManager.Instance.MySpawnerIndex})");
    }
    
    public override void OnConnectedToMaster()
    {
        // 스포너 소유권 다시 확인
        CheckSpawnerOwnership();
        
        // 자동 시작 제거 - IngameManager에서 직접 호출
    }
    
    public override void OnJoinedRoom()
    {
        // 방에 입장하면 스포너 소유권 확인
        CheckSpawnerOwnership();
        
        // 자동 시작 제거 - IngameManager에서 직접 호출
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 다른 플레이어가 입장하면 스포너 소유권 다시 확인
        CheckSpawnerOwnership();
    }
    
    IEnumerator StartRound(int round)
    {
        if (isSpawning)
            yield break;
            
        // 이 스포너가 내 것이 아니면 중단
        if (!isMySpawner)
            yield break;
            
        isSpawning = true;
        monstersSpawnedThisRound = 0;
        
        
        // 해당 라운드의 몬스터 수 가져오기
        int monstersToSpawn = GetMonstersForRound(round);
        
        Debug.Log($"[스포너 {SpawnerIndex}] 라운드 {round} 시작: {monstersToSpawn}마리 스폰 예정");
        
        // 몬스터 스폰
        for (int i = 0; i < monstersToSpawn; i++)
        {
            // 이 스포너가 내 것이 아니면 중단
            if (!isMySpawner)
            {
                break;
            }
            
            SpawnMonster();
            monstersSpawnedThisRound++;
            
            // 다음 스폰까지 대기
            yield return new WaitForSeconds(spawnInterval);
        }
        
        isSpawning = false;
        Debug.Log($"[스포너 {SpawnerIndex}] 라운드 {round} 완료: {monstersSpawnedThisRound}마리 스폰됨");
    }
    
   void SpawnMonster()
    {
        // MonsterNames를 문자열로 변환하여 프리팹 이름 생성
        string prefabName = MonsterType.ToString();
        
        // Resources 폴더에서 프리팹 로드
        GameObject monsterPrefab = Resources.Load<GameObject>(prefabName);
        
        if (monsterPrefab == null)
        {
            Debug.LogError($"MonsterSpawner: Resources 폴더에서 '{prefabName}' 프리팹을 찾을 수 없습니다!");
            return;
        }
        
        // 스폰 포인트 선택
        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = Quaternion.identity;
        
        // 포톤 네트워크를 통해 몬스터 생성 (프리팹 이름 사용)
        GameObject monster = PhotonNetwork.Instantiate(
            prefabName,
            spawnPosition,
            spawnRotation
        );
        
        if (monster != null)
        {
            Monster monsterComponent = monster.GetComponent<Monster>();
            PhotonView monsterPV = monster.GetComponent<PhotonView>();
            
            // 몬스터의 스포너 인덱스 설정
            monsterComponent.SpanwerIndex = SpawnerIndex;

            // 생성된 몬스터에 MovePoints 전달 (RPC 사용)
            if (monsterPV != null && MovePoints != null && MovePoints.Length > 0)
            {
                // Transform 배열을 Vector3 배열로 변환
                Vector3[] movePointPositions = new Vector3[MovePoints.Length];
                for (int i = 0; i < MovePoints.Length; i++)
                {
                    if (MovePoints[i] != null)
                    {
                        movePointPositions[i] = MovePoints[i].position;
                    }
                }
                
                // 모든 클라이언트에서 MovePoints 설정
                monsterPV.RPC("SetMovePoints", RpcTarget.AllBuffered, movePointPositions);
            }
            
            // 상대 클라이언트의 몬스터인 경우 즉시 위치 조정 (로컬에서만)
            if (monsterPV != null && !monsterPV.IsMine && IngameManager.Instance != null && IngameManager.Instance.TopSpawner != null)
            {
                // 즉시 위치 조정 (OnPhotonInstantiate보다 빠르게)
                StartCoroutine(AdjustMonsterPositionNextFrame(monsterComponent));
            }
            
            Debug.Log($"[스포너 {SpawnerIndex}] 몬스터 스폰됨: {monster.name} at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// 다음 프레임에 몬스터 위치 조정 (즉시 처리)
    /// </summary>
    private System.Collections.IEnumerator AdjustMonsterPositionNextFrame(Monster monster)
    {
        yield return null; // 한 프레임 대기
        
        if (monster != null && IngameManager.Instance != null && IngameManager.Instance.TopSpawner != null)
        {
            MonsterSpawner topSpawner = IngameManager.Instance.TopSpawner;
            
            if (topSpawner.MovePoints != null && topSpawner.MovePoints.Length > 0)
            {
                Vector3[] newMovePointPositions = new Vector3[topSpawner.MovePoints.Length];
                for (int i = 0; i < topSpawner.MovePoints.Length; i++)
                {
                    if (topSpawner.MovePoints[i] != null)
                    {
                        newMovePointPositions[i] = topSpawner.MovePoints[i].position;
                    }
                }
                
                monster.GetComponent<PhotonView>()?.RPC("SetMovePoints", RpcTarget.AllBuffered, newMovePointPositions);
            }
        }
    }
   
    int GetMonstersForRound(int round)
    {
        if (monstersPerRound == null || monstersPerRound.Length == 0)
        {
            return 40; // 기본값
        }
        
        int roundIndex = round - 1; // 라운드 1 = 인덱스 0
        
        if (roundIndex >= 0 && roundIndex < monstersPerRound.Length)
        {
            return monstersPerRound[roundIndex];
        }
        
        // 라운드가 배열 범위를 벗어나면 마지막 값 사용
        return monstersPerRound[monstersPerRound.Length - 1];
    }
    
    // 다음 라운드 시작 (외부에서 호출 가능)
    [PunRPC]
    public void StartNextRound()
    {
        if (isMySpawner && !isSpawning)
        {
            currentRound++;
            StartCoroutine(StartRound(currentRound));
        }
    }
    
    // 공개 메서드: 다음 라운드 시작 (모든 클라이언트에서 호출 가능)
    public void RequestNextRound()
    {
        if (isMySpawner)
        {
            StartNextRound();
        }
        else if (pv != null)
        {
            pv.RPC("StartNextRound", RpcTarget.Others);
        }
    }

    /// <summary>
    /// 외부에서 라운드 시작을 요청할 수 있는 메서드 (IngameManager에서 호출)
    /// </summary>
    public void RequestStartRound()
    {
        if (isMySpawner && !isSpawning)
        {
            StartCoroutine(StartRound(currentRound));
        }
        else
        {
            Debug.Log($"[스포너 {SpawnerIndex}] 스폰 요청 무시 (isMySpawner: {isMySpawner}, isSpawning: {isSpawning})");
        }
    }
    
    // 현재 라운드 정보
    public int GetCurrentRound()
    {
        return currentRound;
    }
    
    public bool IsSpawning()
    {
        return isSpawning;
    }
}