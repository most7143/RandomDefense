using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class MonsterSpawner : MonoBehaviourPunCallbacks
{
    [Header("Monster Settings")]
    [Tooltip("스폰할 몬스터 타입 (Resources 폴더에서 프리팹을 로드합니다)")]
    public MonsterNames MonsterType = MonsterNames.Bird1;

    public int AliveMonsterCount = 0;

    [Header("Spawn Timing")]
    [Tooltip("몬스터 간 스폰 간격 (초)")]
    public float spawnInterval = 0.3f;

    private int currentRound = 1;
    private bool isSpawning = false;
    private int monstersSpawnedCount = 0;

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

        // IngameManager에 스포너 등록
        if (IngameManager.Instance != null)
        {
            IngameManager.Instance.Spawner = this;
        }
    }

    IEnumerator StartRound(int round)
    {
        if (isSpawning)
            yield break;

        // PhotonNetwork가 연결되지 않았으면 스폰하지 않음
        if (!PhotonNetwork.IsConnected)
            yield break;

        isSpawning = true;


        // 해당 라운드의 몬스터 수 가져오기
        int monstersToSpawn = 40;

        Debug.Log($"[스포너] 라운드 {round} 시작: {monstersToSpawn}마리 스폰 예정 (클라이언트: {PhotonNetwork.LocalPlayer.ActorNumber})");

        // 몬스터 스폰
        for (int i = 0; i < monstersToSpawn; i++)
        {
            // PhotonNetwork 연결 확인
            if (!PhotonNetwork.IsConnected)
            {
                break;
            }

            if (monstersSpawnedCount >= IngameManager.Instance.MaxMonsterCount)
            {
                if (IngameManager.Instance != null)
                {
                    IngameManager.Instance.RequestGameFailed();
                }
                
                break;
            }


            SpawnMonster();

            monstersSpawnedCount++;

            IngameManager.Instance.UpdateMonsterCountUI(monstersSpawnedCount);


            // 다음 스폰까지 대기
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;


        // 전체 몬스터 수로 체크
        if (monstersSpawnedCount >= IngameManager.Instance.MaxMonsterCount)
        {
            Debug.Log($"[스포너] 라운드 종료 최대 마리수 도달 (클라이언트: {PhotonNetwork.LocalPlayer.ActorNumber})");
        }
        else
        {
            Debug.Log($"[스포너] 라운드 {round} 완료: {monstersSpawnedCount}마리 스폰됨 (클라이언트: {PhotonNetwork.LocalPlayer.ActorNumber})");
        }
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

            // 생성된 몬스터에 MovePoints 전달 (RPC 사용, 미러링 적용)
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

            Debug.Log($"[스포너] 몬스터 스폰됨: {monster.name} at {spawnPosition} (소유자: {PhotonNetwork.LocalPlayer.ActorNumber})");

            // 내가 소유한 몬스터만 카운트
            if (monsterPV != null && monsterPV.IsMine)
            {
                AliveMonsterCount++;
            }
        }
    }






    /// <summary>
    /// 외부에서 라운드 시작을 요청할 수 있는 메서드 (IngameManager에서 호출)
    /// 각 클라이언트가 자신의 몬스터를 스폰합니다.
    /// </summary>
    public void RequestStartRound()
    {
        // PhotonNetwork가 연결되어 있고, 아직 스폰 중이 아니면 시작
        if (PhotonNetwork.IsConnected && !isSpawning)
        {
            StartCoroutine(StartRound(currentRound));
        }
        else
        {
            Debug.Log($"[스포너] 스폰 요청 무시 (연결: {PhotonNetwork.IsConnected}, isSpawning: {isSpawning})");
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