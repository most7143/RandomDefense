using UnityEngine;
using Photon.Pun;

/// <summary>
/// 미러링 기능을 담당하는 클래스
/// PhotonView를 통해 소유권을 확인하고, 상대방 오브젝트에 미러링 효과를 적용합니다.
/// </summary>
public class MirroringObject : MonoBehaviour
{
    private Vector3 originalPosition; // 원본 위치 (미러링 전)
    private bool originalPositionInitialized = false; // originalPosition 초기화 여부
    private PhotonView pv;

    void Awake()
    {
        // PhotonView 가져오기
        pv = GetComponent<PhotonView>();
        
    
        // 원본 위치 초기화 (스폰 위치 - 항상 원본 좌표로 저장)
        originalPosition = transform.position;
        originalPositionInitialized = true;
        
        // 상대방 오브젝트인 경우 즉시 미러링 적용 (Awake에서 바로 적용하여 스폰과 동시에 미러링)
        if (ShouldApplyMirroring())
        {
            ApplyMirroringPosition();
        }
    }

    void Start()
    {
        // Awake()에서 이미 originalPosition 설정 및 미러링 적용했지만,
        // Start()에서도 다시 확인하여 확실히 적용
        if (!originalPositionInitialized)
        {
            originalPosition = transform.position;
            originalPositionInitialized = true;
        }
        
        if (ShouldApplyMirroring())
        {
            ApplyMirroringPosition();
        }
    }

    /// <summary>
    /// 미러링을 적용해야 하는지 확인
    /// </summary>
    public bool ShouldApplyMirroring()
    {
        if (IngameManager.Instance == null || !IngameManager.Instance.EnableMirroring)
            return false;
            
        // 내 오브젝트가 아니면 미러링 적용 (상대방 오브젝트)
        if (pv == null || pv.IsMine)
            return false;
            
        return true;
    }

    /// <summary>
    /// 원본 위치를 가져옵니다 (미러링 전 좌표)
    /// </summary>
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }

    /// <summary>
    /// 원본 위치를 설정합니다
    /// </summary>
    public void SetOriginalPosition(Vector3 position)
    {
        originalPosition = position;
        originalPositionInitialized = true;
    }

    /// <summary>
    /// 원본 위치를 업데이트합니다 (이동 후 호출)
    /// </summary>
    public void UpdateOriginalPosition(Vector3 newOriginalPosition)
    {
        originalPosition = newOriginalPosition;
    }

    /// <summary>
    /// 미러링된 위치를 적용합니다 (표시용)
    /// </summary>
    public void ApplyMirroringPosition()
    {
        if (!ShouldApplyMirroring())
            return;
            
        // 원본 위치를 미러링하여 표시
        transform.position = IngameManager.Instance.MirrorPosition(originalPosition);
    }

    /// <summary>
    /// 원본 위치로 설정합니다 (내 오브젝트인 경우)
    /// </summary>
    public void ApplyOriginalPosition()
    {
        if (ShouldApplyMirroring())
            return;
            
        transform.position = originalPosition;
    }

    /// <summary>
    /// 위치를 설정합니다 (미러링 여부에 따라 자동 적용)
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        if (ShouldApplyMirroring())
        {
            transform.position = IngameManager.Instance.MirrorPosition(position);
        }
        else
        {
            transform.position = position;
        }
    }

 


    /// <summary>
    /// 미러링 적용 (FixedUpdate 등에서 호출)
    /// </summary>
    public void ApplyMirroring()
    {
        if (ShouldApplyMirroring())
        {
            ApplyMirroringPosition();
        }
    }

    /// <summary>
    /// originalPosition이 초기화되었는지 확인
    /// </summary>
    public bool IsOriginalPositionInitialized()
    {
        return originalPositionInitialized;
    }
}
