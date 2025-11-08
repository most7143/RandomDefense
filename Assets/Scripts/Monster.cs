using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.InputSystem.iOS;

public class Monster : MonoBehaviourPunCallbacks
{
    public int Level;
    public float HP;
    public float MoveSpeed = 2f;

    public Transform[] MovePoints;

    private Animator animator;

    private int currentTargetIndex = 0;
    private const float reachedDistance = 0.1f; // 목표 지점 도달 판정 거리
    private PhotonView pv;
    private Vector3[] movePointPositions; // 동기화된 이동 지점 위치

    private Vector3 targetPosition; // 마스터에서 계산된 목표 위치
    private float syncLerpSpeed = 10f; // 동기화 보간 속도

    void Start()
    {
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("Monster: PhotonView 컴포넌트가 없습니다!");
            return;
        }

        // MovePoints가 설정되지 않았거나 비어있으면 경고
        if (MovePoints == null || MovePoints.Length == 0)
        {
            if (movePointPositions != null && movePointPositions.Length > 0)
            {
                MovePoints = new Transform[movePointPositions.Length];
                for (int i = 0; i < movePointPositions.Length; i++)
                {
                    GameObject pointObj = new GameObject($"MovePoint_{i}");
                    pointObj.transform.position = movePointPositions[i];
                    MovePoints[i] = pointObj.transform;
                }
            }
            else
            {
                Debug.LogWarning("Monster: MovePoints가 설정되지 않았습니다!");
                return;
            }
        }

        // 마스터일 때만 시작 위치 설정
        if (PhotonNetwork.IsMasterClient)
        {
            if (MovePoints.Length > 0 && MovePoints[0] != null)
            {
                transform.position = MovePoints[0].position;
                currentTargetIndex = 1;
            }
        }

        targetPosition = transform.position;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = true; // 꺼져있을 경우 켠다
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // 보이지 않아도 애니메이션 계속 재생
            animator.applyRootMotion = false; // 마스터가 위치를 직접 제어하므로 루트모션 끔
            // animator.updateMode = AnimatorUpdateMode.Normal; // 필요시 설정 가능
        }
        else
        {
            Debug.LogWarning("Monster: Animator를 찾지 못함(자식 포함).");
        }
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            HandleMasterMovement();
        }
        else
        {
            // 다른 클라이언트는 마스터가 보낸 위치로 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * syncLerpSpeed);
        }
    }

    private void HandleMasterMovement()
    {
        if (MovePoints == null || MovePoints.Length == 0)
            return;

        if (currentTargetIndex >= MovePoints.Length || MovePoints[currentTargetIndex] == null)
        {
            currentTargetIndex = 0;
            return;
        }

        Transform targetPoint = MovePoints[currentTargetIndex];
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPoint.position);
        if (distanceToTarget <= reachedDistance)
        {
            currentTargetIndex = (currentTargetIndex + 1) % MovePoints.Length;
        }

        // 위치를 다른 클라이언트에게 전송
        pv.RPC(nameof(SyncPosition), RpcTarget.Others, transform.position);
    }

    [PunRPC]
    private void SyncPosition(Vector3 newPos)
    {
        targetPosition = newPos;
    }

    public void GetMovePoints(Transform[] points)
    {
        MovePoints = points;
    }

    [PunRPC]
    public void SetMovePoints(Vector3[] positions)
    {
        movePointPositions = positions;

        if (positions != null && positions.Length > 0)
        {
            MovePoints = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject pointObj = new GameObject($"MovePoint_{i}");
                pointObj.transform.position = positions[i];
                MovePoints[i] = pointObj.transform;
            }
        }
    }
}
