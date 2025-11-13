using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("레이어 마스크: 플레이어 캐릭터를 선택하기 위한 레이어")]
    public LayerMask CharacterLayerMask = -1;
    
    [Tooltip("레이어 마스크: 타일을 감지하기 위한 레이어")]
    public LayerMask TileLayerMask = -1;
    
    [Tooltip("카메라 참조 (없으면 Camera.main 사용)")]
    public Camera mainCamera;
    
    [Tooltip("타일 그룹 컨트롤러 참조")]
    public TileGroupController TileGroupController;
    
    public PlayerCharacter SelectedCharcter;


    public LineRenderer LineRenderer;
    
    public Material PlayerMaterial;
    public Material OutlineMaterial;

    private Vector3 startDragPosition;
    private Vector3 currentDragPosition;
    private Vector3 startWorldPosition; // 드래그 시작 월드 좌표
    private bool isDragging = false;
    private bool hasSelectedCharacter = false;
    private Tile currentTargetTile = null; // 현재 타겟 타일

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


    RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, CharacterLayerMask);

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
            if (!isDragging)
            {
                // 드래그 시작 시 모든 타일 표시
                isDragging = true;
                ShowAllTiles();
            }
            
            // 타일이 타겟되었을 때만 라인 렌더러 업데이트
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
        
        // 타일 콜라이더 감지
        CheckTileCollider(mouseWorldPos, ref endWorldPosition);
        
        // 타일이 타겟되었을 때만 라인 렌더러 표시
        // 다음 타겟이 정해질 때까지 이전 타겟으로 고정
        if (currentTargetTile != null)
        {
            // 시작 위치는 캐릭터 위치
            Vector3 startPos = startWorldPosition;
            
            // 끝 위치는 현재 타겟 타일 위치 (고정)
            Vector3 targetPos = currentTargetTile.transform.position;
            targetPos.z = SelectedCharcter.transform.position.z;
            
            // LineRenderer 위치 설정
            LineRenderer.SetPosition(0, startPos);
            LineRenderer.SetPosition(1, targetPos);
            
            // LineRenderer 활성화
            LineRenderer.enabled = true;
        }
        else
        {
            // 타일이 타겟되지 않았으면 라인 렌더러 숨김
            LineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 타일 콜라이더 감지 및 타겟 렌더러 제어
    /// </summary>
    private void CheckTileCollider(Vector2 mouseWorldPos, ref Vector3 endWorldPosition)
    {
        // 모든 레이어를 포함한 레이캐스트로 모든 충돌체 가져오기
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero, Mathf.Infinity);
        
        Tile foundTile = null;
        
        // 타일을 우선적으로 찾기
        foreach (RaycastHit2D hit in hits)
        {
            // 타일 레이어 마스크에 포함되어 있는지 확인
            if ((TileLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    foundTile = tile;
                    break; // 타일을 찾으면 즉시 종료
                }
            }
        }
        
        if (foundTile != null)
        {
            // 새 타일로 변경
            if (currentTargetTile != foundTile)
            {
                // 새 타일로 변경 및 타겟 하이라이트 활성화
                currentTargetTile = foundTile;
                ShowTargetHighlight(foundTile.transform.position);
            }
            
            // 도착지점을 타일 중앙으로 고정
            endWorldPosition = foundTile.transform.position;
            endWorldPosition.z = SelectedCharcter.transform.position.z;
        }
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
    /// 모든 타일 표시 (드래그 중)
    /// </summary>
    private void ShowAllTiles()
    {
        if (TileGroupController != null && TileGroupController.Tiles != null)
        {
            foreach (var tile in TileGroupController.Tiles)
            {
                if (tile != null && tile.Renderer != null)
                {
                    tile.Renderer.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// 모든 타일 숨김
    /// </summary>
    private void HideAllTiles()
    {
        if (TileGroupController != null && TileGroupController.Tiles != null)
        {
            foreach (var tile in TileGroupController.Tiles)
            {
                if (tile != null && tile.Renderer != null)
                {
                    tile.Renderer.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// 타겟 하이라이트 표시
    /// </summary>
    private void ShowTargetHighlight(Vector3 position)
    {
        if (TileGroupController != null)
        {
            TileGroupController.ShowTargetTile(position);
        }
    }

    /// <summary>
    /// 타겟 하이라이트 숨김
    /// </summary>
    private void HideTargetHighlight()
    {
        if (TileGroupController != null)
        {
            TileGroupController.HideTargetTile();
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
            // 타일이 타겟으로 설정되어 있지 않으면 이동하지 않음
            if (currentTargetTile == null)
            {
                // 타겟 하이라이트 비활성화
                HideTargetHighlight();
                
                // 모든 타일 숨김
                HideAllTiles();
                
                // 상태 초기화
                isDragging = false;
                hasSelectedCharacter = false;
                
                // 드래그 라인 숨김
                HideDragLine();
                return;
            }
            
            // 타일이 타겟으로 설정되어 있으면 타일 위치로 이동
            Vector3 targetPosition = currentTargetTile.transform.position;
            targetPosition.z = SelectedCharcter.transform.position.z;
            
            SelectedCharcter.MoveTo(targetPosition);
        }
        
        // 타겟 하이라이트 비활성화
        HideTargetHighlight();
        currentTargetTile = null;
        
        // 모든 타일 숨김
        HideAllTiles();
        
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
        
        // 타겟 하이라이트 비활성화
        HideTargetHighlight();
        currentTargetTile = null;
        
        // 모든 타일 숨김
        HideAllTiles();
        
        // 드래그 라인 숨김
        HideDragLine();
    }
}
