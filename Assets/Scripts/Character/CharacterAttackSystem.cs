using UnityEngine;
using Photon.Pun;
using System.Collections;

public class CharacterAttackSystem : MonoBehaviour
{
    [Header("References")]
    public PlayerCharacter PlayerCharacter;
    public SPUM_Prefabs Model;
    public PhotonView PV;

    private float attackCooldown = 0f;
    private const string ENEMY_TAG = "Enemy";

    void Awake()
    {
        // PlayerCharacter 참조 가져오기
        PlayerCharacter = GetComponent<PlayerCharacter>();
        if (PlayerCharacter == null)
        {
            PlayerCharacter = GetComponentInParent<PlayerCharacter>();
        }

        // Model 참조 가져오기
        if (PlayerCharacter != null)
        {
            Model = PlayerCharacter.Model;
        }

        // PhotonView 참조 가져오기
        PV = GetComponent<PhotonView>();
        if (PV == null && PlayerCharacter != null)
        {
            PV = PlayerCharacter.PV;
        }
    }

   void Update()
    {
        // 소유자만 공격 체크 (네트워크 동기화)
        if (PV != null && !PV.IsMine)
            return;

        // 이동 중이면 공격하지 않음
        if (PlayerCharacter != null && PlayerCharacter.IsMoving())
            return;

        // 쿨다운 감소
        if (attackCooldown > 0f)
        {
            attackCooldown -= Time.deltaTime;
        }

        // 쿨다운이 끝났고 범위 내에 적이 있으면 공격
        if (attackCooldown <= 0f && PlayerCharacter != null)
        {
            if (CheckForEnemiesInRange())
            {
                Attack();
                // 공격 속도에 따른 쿨다운 설정 (AttackSpeed가 높을수록 공격 속도가 빠름)
                // AttackSpeed = 1이면 1초마다 공격, AttackSpeed = 2이면 0.5초마다 공격
                attackCooldown = 1f / PlayerCharacter.AttackSpeed;
            }
        }
    }
      /// <summary>
    /// 공격 범위 내에 Enemy 태그를 가진 오브젝트가 있는지 확인 (타일 기준)
    /// </summary>
    private bool CheckForEnemiesInRange()
    {
        if (PlayerCharacter == null)
            return false;

        // 캐릭터가 속한 타일의 중심점 가져오기
        Vector3 attackCenter = GetTileCenterPosition();
        if (attackCenter == Vector3.zero && IngameManager.Instance != null && IngameManager.Instance.TileGroupController != null)
        {
            // 타일을 찾지 못한 경우 캐릭터 위치 사용
            attackCenter = transform.position;
        }

        // 2D 원형 범위로 Enemy 태그를 가진 오브젝트 검색 (타일 중심 기준)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            attackCenter, 
            PlayerCharacter.AttackRange
        );

        // Enemy 태그를 가진 오브젝트가 있는지 확인
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(ENEMY_TAG))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 공격 범위 내의 가장 가까운 적 반환 (타일 기준)
    /// </summary>
    private GameObject GetNearestEnemy()
    {
        if (PlayerCharacter == null)
            return null;

        // 캐릭터가 속한 타일의 중심점 가져오기
        Vector3 attackCenter = GetTileCenterPosition();
        if (attackCenter == Vector3.zero && IngameManager.Instance != null && IngameManager.Instance.TileGroupController != null)
        {
            // 타일을 찾지 못한 경우 캐릭터 위치 사용
            attackCenter = transform.position;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            attackCenter, 
            PlayerCharacter.AttackRange
        );

        GameObject nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(ENEMY_TAG))
            {
                // 타일 중심에서 몬스터까지의 거리 계산
                float distance = Vector2.Distance(attackCenter, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = collider.gameObject;
                }
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// 캐릭터가 속한 타일의 중심 위치를 가져옵니다
    /// </summary>
    private Vector3 GetTileCenterPosition()
    {
        if (PlayerCharacter == null || IngameManager.Instance == null || IngameManager.Instance.TileGroupController == null)
            return Vector3.zero;

        Tile[] tiles = IngameManager.Instance.TileGroupController.Tiles;
        if (tiles == null)
            return Vector3.zero;

        // 모든 타일을 순회하여 이 캐릭터가 속한 타일 찾기
        foreach (Tile tile in tiles)
        {
            if (tile != null && tile.InTilePlayerCharacters != null && tile.InTilePlayerCharacters.Contains(PlayerCharacter))
            {
                // 타일의 중심 위치 반환
                return tile.transform.position;
            }
        }

        return Vector3.zero;
    }
 /// <summary>
    /// 공격 (네트워크 동기화)
    /// </summary>
    public void Attack()
    {
        // 가장 가까운 적 찾기
        GameObject nearestEnemy = GetNearestEnemy();
        
        if (nearestEnemy == null)
            return;

        // 타일 중심 위치 가져오기
        Vector3 attackCenter = GetTileCenterPosition();
        if (attackCenter == Vector3.zero && IngameManager.Instance != null && IngameManager.Instance.TileGroupController != null)
        {
            attackCenter = transform.position;
        }

        // 공격 방향으로 플립 (타일 중심에서 몬스터 방향)
        Vector3 directionToEnemy = (nearestEnemy.transform.position - attackCenter).normalized;
        FlipToDirection(directionToEnemy.x);

        // 애니메이션 재생
        if (Model != null && Model.ATTACK_List != null && Model.ATTACK_List.Count > 0)
        {
            int attackIndex = 0; // 공격 애니메이션 0번 고정
            
            if (PV != null && PV.IsMine)
            {
                // 모든 클라이언트에 애니메이션 동기화
                PV.RPC("SyncPlayAttackAnimation", RpcTarget.All, attackIndex);
            }
            else if (PV == null)
            {
                // PhotonView가 없는 경우 로컬에서만 실행
                Model.PlayAnimation(PlayerState.ATTACK, attackIndex);
            }
        }   

        // 몬스터에게 데미지 입히기
        Monster monster = nearestEnemy.GetComponent<Monster>();
        if (monster != null && PlayerCharacter != null)
        {
            // 네트워크 동기화를 위해 RPC 호출
            if (PV != null && PV.IsMine && monster.PV != null)
            {
                monster.PV.RPC("TakeDamage", RpcTarget.All, PlayerCharacter.Damage);
            }
            else if (PV == null)
            {
                // PhotonView가 없는 경우 로컬에서만 실행
                monster.HP -= PlayerCharacter.Damage;
            }
        }
    }

    /// <summary>
    /// 공격 방향으로 플립
    /// </summary>
    private void FlipToDirection(float directionX)
    {
        if (PlayerCharacter == null)
            return;

        // 방향에 따라 플립 (오른쪽: 정방향, 왼쪽: 반전)
        if (directionX < -0.01f)
        {
            // 왼쪽 - 반전
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX > 0.01f)
        {
            // 오른쪽 - 정방향
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    /// <summary>
    /// 공격 애니메이션 동기화 RPC
    /// </summary>
    [PunRPC]
    private void SyncPlayAttackAnimation(int attackIndex)
    {
        if (Model != null)
        {
            Model.PlayAnimation(PlayerState.ATTACK, attackIndex);
        }
    }

    /// <summary>
    /// 디버그용: 공격 범위 시각화 (에디터에서만) - 타일 기준
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (PlayerCharacter != null)
        {
            Gizmos.color = Color.red;
            Vector3 attackCenter = GetTileCenterPosition();
            if (attackCenter == Vector3.zero)
            {
                attackCenter = transform.position;
            }
            Gizmos.DrawWireSphere(attackCenter, PlayerCharacter.AttackRange);
        }
    }
}
