using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
public class Monster : MonoBehaviourPunCallbacks
{
    public MonsterNames Name;

    public int SpanwerIndex = 0;

    public int Level;
    public float HP;
    public float MaxHP; // 최대 체력 저장
    public float MoveSpeed = 2f;

    public SpriteRenderer SpriteRenderer;
    public PhotonView PV;

    public GameObject HPBar;

    public Image HPBarImage;
    private int currentTargetIndex = 0;
    private const float reachedDistance = 0.1f;
    
    private Vector3[] movePointPositions;
    private bool positionAdjusted = false; // 위치 조정 여부

    void Awake()
    {
        // PhotonView 초기화 대기
        PV = GetComponent<PhotonView>();
    }

    /// <summary>
    /// Photon이 오브젝트를 인스턴스화한 직후 호출 (매우 빠른 타이밍)
    /// </summary>
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // PhotonView가 있고 상대 클라이언트의 몬스터인 경우 즉시 위쪽으로 이동
        if (PV != null && !PV.IsMine && IngameManager.Instance != null)
        {
            AdjustPositionForOpponent();
        }
    }

    void Start()
    {
       // 최대 체력 저장 (초기 체력을 최대 체력으로 설정)
        if (MaxHP <= 0)
        {
            MaxHP = HP;
        }

        // HPBar 초기에는 비활성화
        if (HPBar != null)
        {
            HPBar.SetActive(false);
        }

        // OnPhotonInstantiate가 호출되지 않은 경우를 대비한 백업
        if (!positionAdjusted && PV != null && !PV.IsMine && IngameManager.Instance != null)
        {
            AdjustPositionForOpponent();
        }
    }

    /// <summary>
    /// 상대 클라이언트의 몬스터를 위쪽 스포너 위치로 이동하고 MovePoints 갱신
    /// </summary>
    private void AdjustPositionForOpponent()
    {
        if (positionAdjusted || IngameManager.Instance == null)
            return;

        // IngameManager의 캐싱된 위쪽 스포너 사용
        if (IngameManager.Instance.TopSpawner != null)
        {
            MonsterSpawner topSpawner = IngameManager.Instance.TopSpawner;
            
            // 위치 변경
            transform.position = topSpawner.transform.position;
            SpanwerIndex = 1; // 위쪽으로 표시
            positionAdjusted = true;
            
            // 위쪽 스포너의 MovePoints로 갱신
            if (topSpawner.MovePoints != null && topSpawner.MovePoints.Length > 0)
            {
                // Transform 배열을 Vector3 배열로 변환
                Vector3[] newMovePointPositions = new Vector3[topSpawner.MovePoints.Length];
                for (int i = 0; i < topSpawner.MovePoints.Length; i++)
                {
                    if (topSpawner.MovePoints[i] != null)
                    {
                        newMovePointPositions[i] = topSpawner.MovePoints[i].position;
                    }
                }
                
                // MovePoints 갱신 (즉시 처리)
                movePointPositions = newMovePointPositions;
                if (newMovePointPositions != null && newMovePointPositions.Length > 0)
                {
                    transform.position = newMovePointPositions[0];
                    currentTargetIndex = 1 % newMovePointPositions.Length;
                }
                
                Debug.Log($"[Monster] 상대 몬스터를 위쪽 스포너로 즉시 이동 및 MovePoints 갱신: {newMovePointPositions.Length}개 포인트");
            }
            else
            {
                Debug.LogWarning($"[Monster] 위쪽 스포너의 MovePoints가 없습니다!");
            }
        }
    }

    void FixedUpdate()
    {
        if (movePointPositions == null || movePointPositions.Length == 0)
            return;

        // 모든 클라이언트에서 동일한 이동 로직 실행 (동기화 불필요)
        HandleMovement();
    }

    private void HandleMovement()
    {


        
        if (currentTargetIndex >= movePointPositions.Length)
        {
            currentTargetIndex = 0;
            return;
        }

        Vector3 targetPoint = movePointPositions[currentTargetIndex];
        Vector3 direction = targetPoint - transform.position;

        // 이동 (FixedUpdate이므로 Time.fixedDeltaTime 사용)
        transform.position += direction.normalized * MoveSpeed * Time.fixedDeltaTime;

        Filp(direction.x);

        // 목표 지점 도달 체크
        if (direction.magnitude <= reachedDistance)
        {
            currentTargetIndex = (currentTargetIndex + 1) % movePointPositions.Length;
        }
    }

    private void Filp(float x)
    {
        if (SpriteRenderer != null && Mathf.Abs(x) > 0.001f)
        {
            SpriteRenderer.flipX = x < 0f;   // 왼쪽 이동 시 flip
        }
    }

    /// <summary>
    /// 데미지를 받는 RPC 메서드
    /// </summary>
    [PunRPC]
    public void TakeDamage(float damage)
    {
        float previousHP = HP;
        HP -= damage;
        
        // 체력이 감소했고, 이전 체력이 최대 체력이었다면 HPBar 표시
        if (previousHP >= MaxHP && HP < MaxHP && HPBar != null)
        {
            HPBar.SetActive(true);
        }
        
        // HPBar의 FillAmount 갱신
        UpdateHPBar();
        
        // 체력이 0 이하가 되면 몬스터 제거
        if (HP <= 0)
        {
            HP = 0;
            
            // 스포너의 카운터 감소 및 갱신
            DecreaseSpawnerCount();
            
            // 몬스터 파괴 (네트워크 동기화)
            if (PV != null && PV.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else if (PV == null)
            {
                // PhotonView가 없는 경우 로컬에서만 파괴
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// HPBar의 FillAmount를 현재 체력에 비례하여 갱신
    /// </summary>
    private void UpdateHPBar()
    {
        if (HPBarImage == null || MaxHP <= 0)
            return;

        // 현재 체력 / 최대 체력 비율 계산 (0 ~ 1 사이)
        float fillAmount = Mathf.Clamp01(HP / MaxHP);
        HPBarImage.fillAmount = fillAmount;
    }

    /// <summary>
    /// 스포너의 카운터 감소 및 갱신
    /// </summary>
    private void DecreaseSpawnerCount()
    {
        if (IngameManager.Instance == null)
            return;

        // SpanwerIndex로 해당 스포너 찾기
        MonsterSpawner spawner = null;
        if (SpanwerIndex == 1)
        {
            spawner = IngameManager.Instance.TopSpawner;
        }
        else if (SpanwerIndex == 2)
        {
            spawner = IngameManager.Instance.DownSpawner;
        }

        // 스포너를 찾았고 카운터가 0보다 크면 감소
        if (spawner != null && spawner.AliveMonsterCount > 0)
        {
            spawner.AliveMonsterCount--;
            Debug.Log($"[Monster] 스포너 {SpanwerIndex}의 생존 몬스터 수 감소: {spawner.AliveMonsterCount}");
        }
    }
}