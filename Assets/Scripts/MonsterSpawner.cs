using UnityEngine;
using Photon.Pun;
using System.Collections;

public class MonsterSpawner : MonoBehaviourPunCallbacks
{
    [Header("Monster Settings")]
    [Tooltip("스폰할 몬스터 프리팹 (PhotonView가 있어야 함)")]
    public GameObject monsterPrefab;
    
    [Header("Spawn Settings")]
    [Tooltip("스폰 포인트들 (Transform 배열)")]
    public Transform[] spawnPoints;
    
    [Tooltip("라운드별 몬스터 수 (인덱스 0 = 라운드 1)")]
    public int[] monstersPerRound = new int[] { 40 };
    
    [Header("Spawn Timing")]
    [Tooltip("몬스터 간 스폰 간격 (초)")]
    public float spawnInterval = 0.5f;
    
    [Tooltip("라운드 시작 후 스폰 시작까지 대기 시간 (초)")]
    public float roundStartDelay = 2f;
    
    private int currentRound = 1;
    private bool isSpawning = false;
    private int monstersSpawnedThisRound = 0;

    [Header("Move Points")]
    [Tooltip("몬스터 이동 경로 포인트들")]
    public Transform[] MovePoints;
    
    private PhotonView pv;
    
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
        
        // 포톤에 연결되어 있고 이 스포너를 소유한 클라이언트인 경우에만 시작
        if (PhotonNetwork.IsConnected && pv.IsMine)
        {
            StartCoroutine(StartRound(currentRound));
        }
        else if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("MonsterSpawner: 포톤에 연결 중...");
        }
    }
    
    public override void OnConnectedToMaster()
    {
        // 포톤에 연결되면 스폰 시작
        if (pv != null && pv.IsMine && !isSpawning)
        {
            StartCoroutine(StartRound(currentRound));
        }
    }
    
    IEnumerator StartRound(int round)
    {
        if (isSpawning)
            yield break;
            
        // 이 스포너를 소유한 클라이언트가 아니면 중단
        if (pv == null || !pv.IsMine)
            yield break;
            
        isSpawning = true;
        monstersSpawnedThisRound = 0;
        
        // 라운드 시작 대기
        yield return new WaitForSeconds(roundStartDelay);
        
        // 해당 라운드의 몬스터 수 가져오기
        int monstersToSpawn = GetMonstersForRound(round);
        
        Debug.Log($"[스포너 {PhotonNetwork.LocalPlayer.ActorNumber}] 라운드 {round} 시작: {monstersToSpawn}마리 스폰 예정");
        
        // 몬스터 스폰
        for (int i = 0; i < monstersToSpawn; i++)
        {
            // 이 스포너를 소유한 클라이언트가 아니면 중단
            if (pv == null || !pv.IsMine)
            {
                break;
            }
            
            SpawnMonster();
            monstersSpawnedThisRound++;
            
            // 다음 스폰까지 대기
            yield return new WaitForSeconds(spawnInterval);
        }
        
        isSpawning = false;
        Debug.Log($"[스포너 {PhotonNetwork.LocalPlayer.ActorNumber}] 라운드 {round} 완료: {monstersSpawnedThisRound}마리 스폰됨");
    }
    
   void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("MonsterSpawner: 몬스터 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        // 스폰 포인트 선택
        Vector3 spawnPosition = GetRandomSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;
        
        // 포톤 네트워크를 통해 몬스터 생성
        GameObject monster = PhotonNetwork.Instantiate(
            monsterPrefab.name,
            spawnPosition,
            spawnRotation
        );
        
        if (monster != null)
        {
            // 생성된 몬스터에 MovePoints 전달 (RPC 사용)
            PhotonView monsterPV = monster.GetComponent<PhotonView>();
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
            
            Debug.Log($"[스포너 {PhotonNetwork.LocalPlayer.ActorNumber}] 몬스터 스폰됨: {monster.name} at {spawnPosition}");
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // 랜덤 스폰 포인트 선택
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (randomSpawnPoint != null)
            {
                return randomSpawnPoint.position;
            }
        }
        
        // 스폰 포인트가 없으면 현재 위치 주변에 랜덤하게 스폰
        Vector3 randomOffset = new Vector3(
            Random.Range(-5f, 5f),
            0f,
            Random.Range(-5f, 5f)
        );
        return transform.position + randomOffset;
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
        if (pv != null && pv.IsMine && !isSpawning)
        {
            currentRound++;
            StartCoroutine(StartRound(currentRound));
        }
    }
    
    // 공개 메서드: 다음 라운드 시작 (모든 클라이언트에서 호출 가능)
    public void RequestNextRound()
    {
        if (pv != null && pv.IsMine)
        {
            StartNextRound();
        }
        else if (pv != null)
        {
            pv.RPC("StartNextRound", RpcTarget.Others);
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