using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("레이어 마스크: 플레이어 캐릭터를 선택하기 위한 레이어")]
    public LayerMask characterLayerMask = -1;
    
    [Tooltip("카메라 참조 (없으면 Camera.main 사용)")]
    public Camera mainCamera;
    
    public PlayerCharcter SelectedCharcter;

    private Vector3 startDragPosition;
    private Vector3 currentDragPosition;
    private bool isDragging = false;
    private bool hasSelectedCharacter = false;

    void Start()
    {
        // 카메라가 없으면 메인 카메라 사용
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 입력 처리 (마우스 클릭 및 드래그)
    /// </summary>
    private void HandleInput()
    {
        // 마우스 왼쪽 버튼을 눌렀을 때
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }
        
        // 마우스를 드래그하는 중
        if (Input.GetMouseButton(0) && hasSelectedCharacter)
        {
            OnMouseDrag();
        }
        
        // 마우스 버튼을 뗐을 때
        if (Input.GetMouseButtonUp(0))
        {
            OnMouseUp();
        }
    }

    /// <summary>
    /// 마우스 버튼을 눌렀을 때 (캐릭터 선택)
    /// </summary>
    private void OnMouseDown()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Debug.Log($"[PlayerController] OnMouseDown: {ray.origin}, {ray.direction}");

        // 레이캐스트로 모든 충돌체 검출
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, characterLayerMask);
        
        // 거리순으로 정렬 (가장 가까운 것부터)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        // 검출된 오브젝트 중 플레이어 찾기
        PlayerCharcter foundCharacter = null;
        
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"[PlayerController] 검출된 오브젝트: {hit.collider.name}, 거리: {hit.distance}");
            
            PlayerCharcter character = hit.collider.GetComponent<PlayerCharcter>();
            if (character != null)
            {
                foundCharacter = character;
                Debug.Log($"[PlayerController] 플레이어 발견: {character.Name}");
                break; // 가장 가까운 플레이어를 찾으면 중단
            }
        }
        
        // 플레이어를 찾았으면 선택
        if (foundCharacter != null)
        {
            SelectCharcter(foundCharacter);
            startDragPosition = Input.mousePosition;
            hasSelectedCharacter = true;
            isDragging = false;
        }
        else
        {
            // 플레이어가 없으면 선택 해제
            DeselectCharacter();
        }
    }

    /// <summary>
    /// 마우스 드래그 중
    /// </summary>
    private void OnMouseDrag()
    {
        if (SelectedCharcter == null)
            return;

        currentDragPosition = Input.mousePosition;
        
        // 드래그 거리가 일정 이상이면 드래그 시작
        float dragDistance = Vector3.Distance(startDragPosition, currentDragPosition);
        if (dragDistance > 10f) // 10픽셀 이상 드래그하면 이동 시작
        {
            isDragging = true;
        }
    }

    /// <summary>
    /// 마우스 버튼을 뗐을 때 (이동 명령)
    /// </summary>
    private void OnMouseUp()
    {
        if (SelectedCharcter == null || !hasSelectedCharacter)
            return;

        if (isDragging)
        {
            // 드래그로 이동 명령
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 지면에 레이캐스트하여 이동 목표 지점 결정
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Vector3 targetPosition = hit.point;
                targetPosition.y = SelectedCharcter.transform.position.y; // Y 좌표는 유지
                SelectedCharcter.MoveTo(targetPosition);
            }
        }
        
        // 상태 초기화
        isDragging = false;
        hasSelectedCharacter = false;
    }

    /// <summary>
    /// 캐릭터 선택
    /// </summary>
    public void SelectCharcter(PlayerCharcter character)
    {
        // 이전 선택 해제
        if (SelectedCharcter != null && SelectedCharcter != character)
        {
            SelectedCharcter.OnDeselected();
        }

        SelectedCharcter = character;
        if (SelectedCharcter != null)
        {
            SelectedCharcter.OnSelected();
        }
    }

    /// <summary>
    /// 캐릭터 선택 해제
    /// </summary>
    private void DeselectCharacter()
    {
        if (SelectedCharcter != null)
        {
            SelectedCharcter.OnDeselected();
        }
        SelectedCharcter = null;
        hasSelectedCharacter = false;
        isDragging = false;
    }
}
