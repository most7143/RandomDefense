using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
public class Monster : MonoBehaviourPunCallbacks
{
    public MonsterNames Name;

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
    public MirroringObject MirroringObject; // 미러링 기능 담당 클래스

    void Awake()
    {
        // PhotonView 초기화 대기
        PV = GetComponent<PhotonView>();
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
        
        // MirroringObject가 이미 Awake에서 처리했지만, Start에서도 확인
        if (MirroringObject != null && !MirroringObject.IsOriginalPositionInitialized())
        {
            MirroringObject.SetOriginalPosition(transform.position);
        }
    }

    void FixedUpdate()
    {
        if (movePointPositions == null || movePointPositions.Length == 0)
            return;

        // 모든 클라이언트에서 동일한 이동 로직 실행 (미러링 적용)
        HandleMovement();
        
        // 미러링 적용 (상대방 몬스터인 경우)
        if (MirroringObject != null)
        {
            MirroringObject.ApplyMirroring();
        }
    }

   private void HandleMovement()
    {
        if (currentTargetIndex >= movePointPositions.Length)
        {
            currentTargetIndex = 0;
            return;
        }

        // 원본 좌표계에서 이동 계산 (모든 클라이언트에서 동일)
        Vector3 originalPosition = MirroringObject != null ? MirroringObject.GetOriginalPosition() : transform.position;
        Vector3 originalTargetPoint = movePointPositions[currentTargetIndex];
        Vector3 direction = originalTargetPoint - originalPosition;

        // 원본 좌표계에서 이동
        originalPosition += direction.normalized * MoveSpeed * Time.fixedDeltaTime;
        
        // MirroringObject에 원본 위치 업데이트
        if (MirroringObject != null)
        {
            MirroringObject.UpdateOriginalPosition(originalPosition);
        }
        
        // 내 몬스터면 실제 위치 설정, 상대방은 미러링 적용 (ApplyMirroring에서)
        if (PV != null && PV.IsMine)
        {
            transform.position = originalPosition;
        }


         Filp(direction.x);

 
        // 목표 지점 도달 체크
        if (direction.magnitude <= reachedDistance)
        {
            currentTargetIndex = (currentTargetIndex + 1) % movePointPositions.Length;
        }
    }
    
    private void Filp(float x)
    {
        // MirroringObject를 사용하지 않는 경우를 위한 폴백
        if (SpriteRenderer != null && Mathf.Abs(x) > 0.001f)
        {
            SpriteRenderer.flipX = x < 0f;
        }
    }
 [PunRPC]
    public void SetMovePoints(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0)
            return;
            
        // 원본 MovePoints 저장 (모든 클라이언트에서 동일한 원본 좌표 사용)
        movePointPositions = positions;
        
        // MirroringObject를 통해 위치 설정
        if (MirroringObject != null)
        {
            // originalPosition이 아직 설정되지 않았다면 현재 위치로 설정
            if (!MirroringObject.IsOriginalPositionInitialized())
            {
                MirroringObject.SetOriginalPosition(transform.position);
            }
            
            // 미러링 여부에 따라 위치 적용
            Vector3 originalPos = MirroringObject.GetOriginalPosition();
            if (MirroringObject.ShouldApplyMirroring())
            {
                MirroringObject.ApplyMirroringPosition();
            }
            else
            {
                MirroringObject.ApplyOriginalPosition();
            }
        }
        else
        {
            // MirroringObject가 없는 경우 폴백
            if (PV != null && !PV.IsMine && IngameManager.Instance != null && IngameManager.Instance.EnableMirroring)
            {
                transform.position = IngameManager.Instance.MirrorPosition(transform.position);
            }
        }
        
        currentTargetIndex = 1 % positions.Length;
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
        if (IngameManager.Instance == null || IngameManager.Instance.Spawner == null)
            return;

        MonsterSpawner spawner = IngameManager.Instance.Spawner;

        // 내 몬스터인 경우에만 카운터 감소
        if (PV != null && PV.IsMine && spawner.AliveMonsterCount > 0)
        {
            spawner.AliveMonsterCount--;
            Debug.Log($"[Monster] 생존 몬스터 수 감소: {spawner.AliveMonsterCount}");
            
            // UI 갱신
            IngameManager.Instance.UpdateMonsterCountUI(spawner.AliveMonsterCount);
        }
    }
}