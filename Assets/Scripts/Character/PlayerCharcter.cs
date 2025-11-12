using UnityEngine;

public class PlayerCharcter : MonoBehaviour
{
    [Header("Character Stats")]
    public CharacterNames Name;
    public float Damage = 1;
    public int AttackSpeed = 1;
    public float AttackRange = 1;

    [Header("Movement Settings")]
    [Tooltip("이동 속도")]
    public float MoveSpeed = 5f;
    
    [Tooltip("목표 지점 도달 판정 거리")]
    public float ReachedDistance = 0.1f;

    [Header("References")]
    public SPUM_Prefabs Model;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isSelected = false;
    private int currentMoveAnimationIndex = 0;

    void Update()
    {
        if (isMoving)
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// 목표 지점까지 이동
    /// </summary>
    public void MoveTo(Vector3 target)
    {
        targetPosition = target;
        targetPosition.y = transform.position.y; // Y 좌표는 유지
        
        isMoving = true;
        
        // 이동 애니메이션 재생
        if (Model != null && Model.MOVE_List != null && Model.MOVE_List.Count > 0)
        {
            // 랜덤하게 이동 애니메이션 선택 (또는 순차적으로)
            currentMoveAnimationIndex = Random.Range(0, Model.MOVE_List.Count);
            Model.PlayAnimation(PlayerState.MOVE, currentMoveAnimationIndex);
        }
        
        // 이동 방향 설정
        UpdateAnimationDirection();
        
        Debug.Log($"[PlayerCharacter] 이동 시작: {transform.position} -> {targetPosition}");
    }

    /// <summary>
    /// 이동 처리
    /// </summary>
    private void HandleMovement()
    {
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;

        // 목표 지점에 도달했는지 확인
        if (distance <= ReachedDistance)
        {
            // 이동 완료
            OnReachedTarget();
            return;
        }

        // 이동 방향으로 이동
        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * MoveSpeed * Time.deltaTime;

        // 이동 방향에 따라 캐릭터 회전 (선택사항)
        if (moveDirection.magnitude > 0.1f)
        {
            // 이동 방향을 바라보도록 회전
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
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
        transform.position = targetPosition; // 정확한 위치로 설정
        
        // 대기 애니메이션 재생
        if (Model != null && Model.IDLE_List != null && Model.IDLE_List.Count > 0)
        {
            int idleIndex = Random.Range(0, Model.IDLE_List.Count);
            Model.PlayAnimation(PlayerState.IDLE, idleIndex);
        }
        
        Debug.Log($"[PlayerCharacter] 이동 완료: {transform.position}");
    }

    /// <summary>
    /// 애니메이션 방향 업데이트 (SPUM 애니메이션 방향 설정)
    /// </summary>
    private void UpdateAnimationDirection()
    {
        if (!isMoving || Model == null)
            return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // 이동 방향에 따라 적절한 애니메이션 방향 선택
        // SPUM은 보통 8방향 애니메이션을 지원합니다
        // 여기서는 간단하게 4방향으로 처리합니다
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 각도에 따라 애니메이션 인덱스 결정 (0-7: 8방향)
        // SPUM의 MOVE_List에 8개의 방향 애니메이션이 있다고 가정
        if (Model.MOVE_List != null && Model.MOVE_List.Count >= 8)
        {
            // 각도를 0-360 범위로 정규화
            if (angle < 0) angle += 360;
            
            // 8방향으로 나누기 (각 45도)
            int directionIndex = Mathf.RoundToInt(angle / 45f) % 8;
            currentMoveAnimationIndex = directionIndex;
            
            Model.PlayAnimation(PlayerState.MOVE, currentMoveAnimationIndex);
        }
        else if (Model.MOVE_List != null && Model.MOVE_List.Count >= 4)
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
            currentMoveAnimationIndex = directionIndex;
            Model.PlayAnimation(PlayerState.MOVE, currentMoveAnimationIndex);
        }
    }

    /// <summary>
    /// 캐릭터가 선택되었을 때 호출
    /// </summary>
    public void OnSelected()
    {
        isSelected = true;
        // 선택 효과 (예: 하이라이트, 아웃라인 등)
        Debug.Log($"[PlayerCharacter] 선택됨: {Name}");
    }

    /// <summary>
    /// 캐릭터 선택이 해제되었을 때 호출
    /// </summary>
    public void OnDeselected()
    {
        isSelected = false;
        // 선택 해제 효과
        Debug.Log($"[PlayerCharacter] 선택 해제: {Name}");
    }

    /// <summary>
    /// 공격
    /// </summary>
    public void Attack()
    {
        if (Model != null && Model.ATTACK_List != null && Model.ATTACK_List.Count > 0)
        {
            int attackIndex = Random.Range(0, Model.ATTACK_List.Count);
            Model.PlayAnimation(PlayerState.ATTACK, attackIndex);
        }
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
}
