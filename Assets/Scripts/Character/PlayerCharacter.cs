using UnityEngine;
using Photon.Pun;

public class PlayerCharacter : MonoBehaviourPunCallbacks
{
    [Header("Character Stats")]
    public CharacterNames Name;
    public float Damage = 1;
    public int AttackSpeed = 1;
    public float AttackRange = 1;
    
    [Tooltip("공격 애니메이션 속도 배율 (1 = 기본 속도, 2 = 2배 빠름)")]
    public float AttackAnimationSpeed = 1f; // 공격 애니메이션 속도 변수 추가

    [Header("Movement Settings")]
    [Tooltip("이동 속도")]
    public float MoveSpeed = 5f;
    
    [Tooltip("목표 지점 도달 판정 거리")]
    public float ReachedDistance = 0.1f;

    [Header("References")]

    public CharacterAttackSystem AttackSystem;
    public SPUM_Prefabs Model;
    public PhotonView PV;

    private Vector3 targetPosition;
    private Vector3 originalTargetPosition; // 원본 타겟 위치 (미러링 전)
    private bool isMoving = false;
    private bool isSelected = false;
    private int currentMoveAnimationIndex = 0;
    
    public MirroringObject MirroringObject; // 미러링 기능 담당 클래스
    
    // 이동 완료 콜백 추가
    public System.Action<PlayerCharacter> OnMoveCompleted;


    public Renderer[] ChangeMaterials;

    void Awake()
    {
        // PhotonView 초기화
        if (PV == null)
        {
            PV = GetComponent<PhotonView>();
        }
        
        // MirroringObject 컴포넌트 가져오기 또는 추가
        if (MirroringObject == null)
        {
            MirroringObject = GetComponent<MirroringObject>();
            if (MirroringObject == null)
            {
                MirroringObject = gameObject.AddComponent<MirroringObject>();
            }
        }
    }

  void Start()
{
    if (Model != null)
    {
        Model.OverrideControllerInit();
    }
    
    // MirroringObject 초기화 확인
    if (MirroringObject != null && !MirroringObject.IsOriginalPositionInitialized())
    {
        MirroringObject.SetOriginalPosition(transform.position);
    }
    
    // 원본 타겟 위치 초기화
    originalTargetPosition = MirroringObject != null ? MirroringObject.GetOriginalPosition() : transform.position;
}

    void Update()
    {
        if (isMoving)
        {
            HandleMovement();
        }
        
        // 미러링 적용 (상대방 캐릭터인 경우)
        if (MirroringObject != null)
        {
            MirroringObject.ApplyMirroring();
        }
    }

    /// <summary>
    /// 목표 지점까지 이동 (네트워크 동기화)
    /// </summary>
    public void MoveTo(Vector3 target)
    {
        // PhotonView가 있고 내가 소유자인 경우에만 RPC 호출
        if (PV != null && PV.IsMine)
        {
            // 모든 클라이언트에 이동 명령 동기화
            PV.RPC("SyncMoveTo", RpcTarget.All, target);
        }
        else if (PV == null)
        {
            // PhotonView가 없는 경우 로컬에서만 실행 (오프라인 모드)
            MoveToLocal(target, false);
        }
    }

    /// <summary>
    /// 이동 실행 (로컬)
    /// </summary>
    private void MoveToLocal(Vector3 target, bool syncAnimation = true)
    {
        // 타겟 위치를 원본 위치로 저장 (미러링 전)
        // 네트워크로 받은 타겟 위치는 항상 원본 좌표 (소유자가 보낸 원본 좌표)
        originalTargetPosition = target;
        
        // 표시용 타겟 위치 설정 (미러링 여부에 따라)
        if (MirroringObject != null && MirroringObject.ShouldApplyMirroring())
        {
            targetPosition = IngameManager.Instance.MirrorPosition(originalTargetPosition);
        }
        else
        {
            targetPosition = originalTargetPosition;
        }
        
        // 2D 게임이므로 Z 좌표는 유지 (깊이 유지)
        Vector3 currentOriginalPos = MirroringObject != null ? MirroringObject.GetOriginalPosition() : transform.position;
        targetPosition.z = transform.position.z;
        originalTargetPosition.z = currentOriginalPos.z;
        
        isMoving = true;
        
         if (Model != null && Model.MOVE_List != null && Model.MOVE_List.Count > 0)
        {
            // 애니메이션이 1개만 있으면 항상 인덱스 0 사용
            currentMoveAnimationIndex = 0;
            Debug.Log("이동 애니메이션 인덱스: " + currentMoveAnimationIndex);
            
            if (syncAnimation && PV != null && PV.IsMine)
            {
                // 네트워크 동기화가 필요한 경우
                PlayMoveAnimation(currentMoveAnimationIndex);
            }
            else
            {
                // 직접 재생 (RPC에서 호출된 경우 또는 PhotonView가 없는 경우)
                if (Model != null)
                {
                    Model.PlayAnimation(PlayerState.MOVE, currentMoveAnimationIndex);
                }
            }
        }
        
        // 이동 방향 설정
        UpdateAnimationDirection();
        
        Debug.Log($"[PlayerCharacter] 이동 시작: {transform.position} -> {targetPosition}");
    }

    /// <summary>
    /// 이동 동기화 RPC
    /// </summary>
    [PunRPC]
    private void SyncMoveTo(Vector3 target)
    {
        // RPC에서 호출되므로 애니메이션은 직접 재생 (syncAnimation = false)
        MoveToLocal(target, false);
    }
    /// <summary>
    /// 이동 처리
    /// </summary>
    private void HandleMovement()
    {
        // 원본 위치 가져오기
        Vector3 originalPosition = MirroringObject != null ? MirroringObject.GetOriginalPosition() : transform.position;
        
        // 원본 위치에서 이동 계산 (미러링 전 원본 위치 사용)
        Vector3 direction = originalTargetPosition - originalPosition;
        float distance = direction.magnitude;

        // 목표 지점에 도달했는지 확인
        if (distance <= ReachedDistance)
        {
            // 이동 완료
            OnReachedTarget();
            return;
        }

        // Lerp를 사용한 부드러운 이동 (원본 위치에서)
        float moveStep = MoveSpeed * Time.deltaTime;
        float lerpSpeed = moveStep / distance; // 거리에 따른 Lerp 속도 조절
        originalPosition = Vector3.Lerp(originalPosition, originalTargetPosition, lerpSpeed);
        
        // MirroringObject에 원본 위치 업데이트
        if (MirroringObject != null)
        {
            MirroringObject.UpdateOriginalPosition(originalPosition);
        }
        
        // 내 캐릭터면 실제 위치 설정, 상대방은 미러링 적용 (ApplyMirroring에서)
        if (PV == null || PV.IsMine)
        {
            transform.position = originalPosition;
        }

        // 이동 방향에 따라 스프라이트 플립 (회전 없이)
        Vector3 moveDirection = direction.normalized;
        if (moveDirection.magnitude > 0.1f)
        {
            // 미러링된 캐릭터인 경우 방향 반전
            float flipDirectionX = moveDirection.x;
            if (MirroringObject != null && MirroringObject.ShouldApplyMirroring())
            {
                flipDirectionX = -flipDirectionX;
            }
            
            // X 방향에 따라 플립 (오른쪽: 1, 왼쪽: -1)
            if (flipDirectionX < 0.01f)
            {
                // 오른쪽으로 이동 - 정방향
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (flipDirectionX > -0.01f)
            {
                // 왼쪽으로 이동 - 반전
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            
            // 애니메이션 방향 업데이트
            UpdateAnimationDirection();
        }
    }
    

    /// <summary>
    /// 목표 지점 도달 시 호출
    /// </summary>
    private void OnReachedTarget()
    {
        isMoving = false;
        
        // MirroringObject에 원본 위치 업데이트
        if (MirroringObject != null)
        {
            MirroringObject.UpdateOriginalPosition(originalTargetPosition);
        }
        
        // 내 캐릭터면 실제 위치 설정, 상대방은 미러링 적용
        if (PV == null || PV.IsMine)
        {
            Vector3 originalPos = MirroringObject != null ? MirroringObject.GetOriginalPosition() : originalTargetPosition;
            transform.position = originalPos;
        }
        else if (MirroringObject != null)
        {
            MirroringObject.ApplyMirroringPosition();
        }
        
        // 대기 애니메이션 재생 (네트워크 동기화)
        if (Model != null && Model.IDLE_List != null && Model.IDLE_List.Count > 0)
        {
            int idleIndex = Random.Range(0, Model.IDLE_List.Count);
            PlayIdleAnimation(idleIndex);
        }
        
        Debug.Log($"[PlayerCharacter] 이동 완료: {transform.position}");
        
        // 이동 완료 콜백 호출 (소유자만)
        if (PV == null || PV.IsMine)
        {
            OnMoveCompleted?.Invoke(this);
        }
    }

    /// <summary>
    /// 대기 애니메이션 재생 (네트워크 동기화)
    /// </summary>
    private void PlayIdleAnimation(int idleIndex)
    {
        if (PV != null && PV.IsMine)
        {
            // 모든 클라이언트에 애니메이션 동기화
            PV.RPC("SyncPlayAnimation", RpcTarget.All, (int)PlayerState.IDLE, idleIndex);
        }
        else
        {
            // PhotonView가 없거나 소유자가 아닌 경우 직접 재생
            if (Model != null)
            {
                Model.PlayAnimation(PlayerState.IDLE, idleIndex);
            }
        }
    }

  /// <summary>
    /// 애니메이션 방향 업데이트 (SPUM 애니메이션 방향 설정)
    /// </summary>
    private void UpdateAnimationDirection()
    {
        if (!isMoving || Model == null)
            return;

        // 애니메이션이 1개만 있으면 방향 업데이트 없이 인덱스 0 사용
        if (Model.MOVE_List == null || Model.MOVE_List.Count <= 1)
        {
            currentMoveAnimationIndex = 0;
            return;
        }

        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // 이동 방향에 따라 적절한 애니메이션 방향 선택
        // SPUM은 보통 8방향 애니메이션을 지원합니다
        // 여기서는 간단하게 4방향으로 처리합니다
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 각도에 따라 애니메이션 인덱스 결정 (0-7: 8방향)
        // SPUM의 MOVE_List에 8개의 방향 애니메이션이 있다고 가정
        if (Model.MOVE_List.Count >= 8)
        {
            // 각도를 0-360 범위로 정규화
            if (angle < 0) angle += 360;
            
            // 8방향으로 나누기 (각 45도)
            int directionIndex = Mathf.RoundToInt(angle / 45f) % 8;
            int newAnimationIndex = Mathf.Clamp(directionIndex, 0, Model.MOVE_List.Count - 1);
            
            // 방향이 변경된 경우에만 애니메이션 업데이트
            if (currentMoveAnimationIndex != newAnimationIndex)
            {
                currentMoveAnimationIndex = newAnimationIndex;
                // 이동 중 방향 변경은 각 클라이언트에서 독립적으로 처리
                if (Model != null)
                {
                    Model.PlayAnimation(PlayerState.MOVE, currentMoveAnimationIndex);
                }
            }
        }
        else if (Model.MOVE_List.Count >= 4)
        {
            // 4방향으로 처리
            int directionIndex = 0;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // 좌우
                directionIndex = direction.x > 0 ? 0 : 2; // 오른쪽: 0, 왼쪽: 2
            }
            else
            {
                // 상하
                directionIndex = direction.y > 0 ? 1 : 3; // 위: 1, 아래: 3
            }
            int newAnimationIndex = Mathf.Clamp(directionIndex, 0, Model.MOVE_List.Count - 1);
            
            // 방향이 변경된 경우에만 애니메이션 업데이트
            if (currentMoveAnimationIndex != newAnimationIndex)
            {
                currentMoveAnimationIndex = newAnimationIndex;
                // 이동 중 방향 변경은 각 클라이언트에서 독립적으로 처리
                if (Model != null)
                {
                    Model.PlayAnimation(PlayerState.MOVE, currentMoveAnimationIndex);
                }
            }
        }
        else
        {
            // 애니메이션이 2-3개만 있을 때는 인덱스 0 사용
            currentMoveAnimationIndex = 0;
        }
    }

    /// <summary>
    /// 이동 애니메이션 재생 (네트워크 동기화)
    /// </summary>
    private void PlayMoveAnimation(int animationIndex)
    {
        if (PV != null && PV.IsMine)
        {
            // 모든 클라이언트에 애니메이션 동기화
            PV.RPC("SyncPlayAnimation", RpcTarget.All, (int)PlayerState.MOVE, animationIndex);
        }
        else
        {
            // PhotonView가 없거나 소유자가 아닌 경우 직접 재생
            if (Model != null)
            {
                Model.PlayAnimation(PlayerState.MOVE, animationIndex);
            }
        }
    }

    /// <summary>
    /// 애니메이션 동기화 RPC
    /// </summary>
    [PunRPC]
    private void SyncPlayAnimation(int state, int animationIndex)
    {
        if (Model != null)
        {
            Model.PlayAnimation((PlayerState)state, animationIndex);
        }
    }

    /// <summary>
    /// 캐릭터가 선택되었을 때 호출
    /// </summary>
    public void OnSelected()
    {
        isSelected = true;
        Debug.Log($"[PlayerCharacter] 선택됨: {Name}");
    }

    /// <summary>
    /// 캐릭터 선택이 해제되었을 때 호출
    /// </summary>
    public void OnDeselected()
    {
        isSelected = false;
        Debug.Log($"[PlayerCharacter] 선택 해제: {Name}");
    }

    /// <summary>
    /// 공격 (네트워크 동기화)
    /// </summary>
    public void Attack()
    {
        if (Model != null && Model.ATTACK_List != null && Model.ATTACK_List.Count > 0)
        {
            int attackIndex = Random.Range(0, Model.ATTACK_List.Count);
            
            if (PV != null && PV.IsMine)
            {
                // 모든 클라이언트에 애니메이션 동기화
                PV.RPC("SyncPlayAnimation", RpcTarget.All, (int)PlayerState.ATTACK, attackIndex);
            }
            else if (PV == null)
            {
                // PhotonView가 없는 경우 로컬에서만 실행
                Model.PlayAnimation(PlayerState.ATTACK, attackIndex);
            }
        }
    }

    /// <summary>
    /// 소환 위치 설정 (네트워크 동기화)
    /// </summary>
    [PunRPC]
    public void SetSpawnPosition(Vector3 position)
    {
        // MirroringObject에 원본 위치 설정
        if (MirroringObject != null)
        {
            MirroringObject.SetOriginalPosition(position);
            if (MirroringObject.ShouldApplyMirroring())
            {
                MirroringObject.ApplyMirroringPosition();
            }
            else
            {
                transform.position = position;
            }
        }
        else
        {
            transform.position = position;
        }
        
        originalTargetPosition = position;
        Debug.Log($"[PlayerCharacter] 소환 위치 설정: {position}");
    }

    /// <summary>
    /// 캐릭터 활성화 (네트워크 동기화) - Renderer/Collider 활성화
    /// </summary>
    [PunRPC]
    public void SetCharacterSpawnedSync()
    {
        // GameObject 활성화 (항상 활성화 상태 유지)
        gameObject.SetActive(true);

        // Model 초기화
        if (Model != null)
        {
            Model.OverrideControllerInit();
        }

        // Renderer 활성화
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        // Collider 활성화
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        // PlayerCharacter 컴포넌트 활성화
        enabled = true;

        // MirroringObject 초기화 확인
        if (MirroringObject != null)
        {
            // 원본 위치가 초기화되지 않았다면 현재 위치로 설정
            if (!MirroringObject.IsOriginalPositionInitialized())
            {
                MirroringObject.SetOriginalPosition(transform.position);
            }

            // 상대방 캐릭터인 경우 즉시 미러링 적용
            if (MirroringObject.ShouldApplyMirroring())
            {
                MirroringObject.ApplyMirroringPosition();
            }
        }

        Debug.Log($"[PlayerCharacter] 캐릭터 활성화 동기화 완료: {Name}");
    }

    /// <summary>
    /// 이동 중인지 확인
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// 선택되었는지 확인
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }


    public void SetMaterial(Material material)
    {
        foreach (var mat in ChangeMaterials)
        {
            mat.material = material;
        }
    }

}
