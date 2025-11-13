using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("레이어 마스크: 플레이어 캐릭터를 선택하기 위한 레이어")]
    public LayerMask characterLayerMask = -1;
    
    [Tooltip("카메라 참조 (없으면 Camera.main 사용)")]
    public Camera mainCamera;
    
    public PlayerCharacter SelectedCharcter;


    public LineRenderer LineRenderer;
    
    public Material PlayerMaterial;
    public Material OutlineMaterial;

    private Vector3 startDragPosition;
    private Vector3 currentDragPosition;
    private Vector3 startWorldPosition; // 드래그 시작 월드 좌표
    private bool isDragging = false;
    private bool hasSelectedCharacter = false;

    void Start()
    {
        // 카메라가 없으면 메인 카메라 사용
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // LineRenderer 초기화
        InitializeLineRenderer();
    }
    
    private void InitializeLineRenderer()
    {
      
        // LineRenderer 설정
        LineRenderer.positionCount = 2;
        LineRenderer.startWidth = 0.05f;
        LineRenderer.endWidth = 0.05f;
        LineRenderer.useWorldSpace = true;
        LineRenderer.enabled = false;
      
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
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);


    RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, characterLayerMask);

    if (hit.collider != null)
    {

        PlayerCharacter character = hit.collider.GetComponent<PlayerCharacter>();
        if (character != null)
        {
            SelectCharcter(character);
            startDragPosition = Input.mousePosition;
            startWorldPosition = character.transform.position;
            hasSelectedCharacter = true;
            isDragging = false;
            return;
        }
    }

    // 아무것도 선택되지 않으면 해제
    DeselectCharacter();
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
            
            // LineRenderer 업데이트
            UpdateDragLine();
        }
        else
        {
            // 드래그 거리가 부족하면 라인 숨김
            HideDragLine();
        }
    }
    
    /// <summary>
    /// 드래그 라인 업데이트
    /// </summary>
    private void UpdateDragLine()
    {
        if (LineRenderer == null || SelectedCharcter == null)
            return;
        
        // 현재 마우스 위치를 월드 좌표로 변환
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 endWorldPosition = new Vector3(mouseWorldPos.x, mouseWorldPos.y, SelectedCharcter.transform.position.z);
        
        // 시작 위치는 캐릭터 위치
        Vector3 startPos = startWorldPosition;
        
        // LineRenderer 위치 설정
        LineRenderer.SetPosition(0, startPos);
        LineRenderer.SetPosition(1, endWorldPosition);
        
        // LineRenderer 활성화
        LineRenderer.enabled = true;
    }
    
    /// <summary>
    /// 드래그 라인 숨김
    /// </summary>
    private void HideDragLine()
    {
        if (LineRenderer != null)
        {
            LineRenderer.enabled = false;
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
            // 드래그로 이동 명령 (2D)
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            // 2D 레이캐스트로 이동 목표 지점 결정
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, Mathf.Infinity);
            
            if (hit.collider != null)
            {
                Vector3 targetPosition = hit.point;
                // Z 좌표는 캐릭터의 현재 Z 좌표 유지 (2D 게임에서 깊이 유지)
                targetPosition.z = SelectedCharcter.transform.position.z;
                SelectedCharcter.MoveTo(targetPosition);
            }
            else
            {
                // 레이캐스트에 맞지 않으면 마우스 위치로 직접 이동
                Vector3 targetPosition = mouseWorldPos;
                targetPosition.z = SelectedCharcter.transform.position.z;
                SelectedCharcter.MoveTo(targetPosition);
            }
        }
        
        // 상태 초기화
        isDragging = false;
        hasSelectedCharacter = false;
        
        // 드래그 라인 숨김
        HideDragLine();
    }

    /// <summary>
    /// 캐릭터 선택
    /// </summary>
    public void SelectCharcter(PlayerCharacter character)
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

        SelectedCharcter.SetMaterial(OutlineMaterial);
    }

    /// <summary>
    /// 캐릭터 선택 해제
    /// </summary>
    private void DeselectCharacter()
    {
        if (SelectedCharcter != null)
        {
            SelectedCharcter.SetMaterial(PlayerMaterial);
            SelectedCharcter.OnDeselected();
        }
        SelectedCharcter = null;
        hasSelectedCharacter = false;
        isDragging = false;
        
        // 드래그 라인 숨김
        HideDragLine();
    }
}
