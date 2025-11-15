using UnityEngine;
using System.Collections.Generic;

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
    private Tile startTile = null; // 이동 시작 타일
    private Tile pendingTargetTile = null; // 이동 완료 후 추가할 타일 (이동 중 타일 리스트 갱신 방지용)
    private List<PlayerCharacter> charactersToMove = new List<PlayerCharacter>(); // 이동할 캐릭터 목록
    private HashSet<PlayerCharacter> movingCharactersWithOutline = new HashSet<PlayerCharacter>(); // 아웃라인을 유지해야 하는 이동 중인 캐릭터들

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
    /// 마우스 버튼을 눌렀을 때 (타일 클릭으로 캐릭터 선택)
    /// </summary>
   private void OnMouseDown()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 먼저 타일을 클릭했는지 확인
        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero, Mathf.Infinity);
        
        Tile clickedTile = null;
        
        // 타일을 우선적으로 찾기
        foreach (RaycastHit2D hit in hits)
        {
            // 타일 레이어 마스크에 포함되어 있는지 확인
            if ((TileLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    clickedTile = tile;
                    break; // 타일을 찾으면 즉시 종료
                }
            }
        }
        
        // 타일을 클릭했고, 그 타일 안에 캐릭터가 있는 경우
        if (clickedTile != null && clickedTile.InTilePlayerCharacters != null && clickedTile.InTilePlayerCharacters.Count > 0)
        {
            // 타일 안의 첫 번째 캐릭터를 선택 (표시용)
            PlayerCharacter character = clickedTile.InTilePlayerCharacters[0];
            if (character != null)
            {
                SelectCharcter(character);
                startDragPosition = Input.mousePosition;
                startWorldPosition = character.transform.position;
                startTile = clickedTile; // 시작 타일 저장
                hasSelectedCharacter = true;
                isDragging = false;
                pendingTargetTile = null;
                charactersToMove.Clear();
                
                // 모든 캐릭터에 이동 완료 콜백 등록
                foreach (var charInTile in clickedTile.InTilePlayerCharacters)
                {
                    if (charInTile != null)
                    {
                        charInTile.OnMoveCompleted = OnCharacterMoveCompleted;
                    }
                }
                return;
            }
        }

       // 타일을 클릭하지 않았거나 타일 안에 캐릭터가 없으면 선택 해제
       startTile = null;
       pendingTargetTile = null;
       charactersToMove.Clear();
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
                startTile = null;
                pendingTargetTile = null;
                charactersToMove.Clear();
                movingCharactersWithOutline.Clear();
                
                // 모든 캐릭터의 콜백 해제
                ClearCharacterCallbacks();
                
                // 드래그 라인 숨김
                HideDragLine();
                return;
            }
            
            // 시작 타일과 목표 타일이 다른 경우
            if (startTile != null && startTile != currentTargetTile)
            {
                // 이동할 캐릭터 목록 저장 (복사본 생성)
                charactersToMove = new List<PlayerCharacter>(startTile.InTilePlayerCharacters);
                
                // 목표 타일에 이미 캐릭터가 있는지 확인 (스왑용)
                List<PlayerCharacter> targetTileCharacters = new List<PlayerCharacter>(currentTargetTile.InTilePlayerCharacters);
                bool shouldSwap = targetTileCharacters.Count > 0;
                
                // 이동 시작 전에 아웃라인이 있는 캐릭터들 추적 (3마리였던 경우)
                if (startTile.InTilePlayerCharacters.Count == 3)
                {
                    foreach (var character in charactersToMove)
                    {
                        if (character != null)
                        {
                            movingCharactersWithOutline.Add(character);
                        }
                    }
                }
                
                // 목표 타일의 캐릭터들도 아웃라인 추적 (3마리였던 경우)
                if (shouldSwap && targetTileCharacters.Count == 3)
                {
                    foreach (var character in targetTileCharacters)
                    {
                        if (character != null)
                        {
                            movingCharactersWithOutline.Add(character);
                        }
                    }
                }
                
                // 1. 시작 타일에서 캐릭터 제거 (즉시)
                foreach (var character in charactersToMove)
                {
                    if (character != null)
                    {
                        startTile.RemoveInTilePlayerCharacter(character);
                    }
                }
                
                // 2. 목표 타일에서 캐릭터 제거 (스왑용, 즉시)
                if (shouldSwap)
                {
                    foreach (var character in targetTileCharacters)
                    {
                        if (character != null)
                        {
                            currentTargetTile.RemoveInTilePlayerCharacter(character);
                        }
                    }
                }
                
                // 3. 목표 타일에 시작 타일의 캐릭터 추가 (즉시)
                pendingTargetTile = currentTargetTile; // 추적용으로 저장
                
                for (int i = 0; i < charactersToMove.Count; i++)
                {
                    PlayerCharacter character = charactersToMove[i];
                    if (character != null)
                    {
                        // 타일 리스트에 추가 (위치 갱신은 하지 않음)
                        if (!currentTargetTile.InTilePlayerCharacters.Contains(character))
                        {
                            currentTargetTile.InTilePlayerCharacters.Add(character);
                        }
                    }
                }
                
                // 4. 시작 타일에 목표 타일의 캐릭터 추가 (스왑용, 즉시)
                if (shouldSwap)
                {
                    for (int i = 0; i < targetTileCharacters.Count; i++)
                    {
                        PlayerCharacter character = targetTileCharacters[i];
                        if (character != null)
                        {
                            // 타일 리스트에 추가 (위치 갱신은 하지 않음)
                            if (!startTile.InTilePlayerCharacters.Contains(character))
                            {
                                startTile.InTilePlayerCharacters.Add(character);
                            }
                            
                            // 시작 타일로 이동하는 캐릭터의 콜백 등록
                            character.OnMoveCompleted = OnCharacterMoveCompleted;
                            
                            // 스왑된 캐릭터들도 이동 완료 추적에 추가
                            if (!charactersToMove.Contains(character))
                            {
                                charactersToMove.Add(character);
                            }
                        }
                    }
                }
                
                // 5. 각 캐릭터의 목표 위치 계산 및 이동 시작 (시작 타일 -> 목표 타일)
                for (int i = 0; i < charactersToMove.Count; i++)
                {
                    PlayerCharacter character = charactersToMove[i];
                    if (character != null)
                    {
                        // 캐릭터가 목표 타일에 속하는지 확인
                        bool isMovingToTarget = currentTargetTile.InTilePlayerCharacters.Contains(character);
                        Tile targetTile = isMovingToTarget ? currentTargetTile : startTile;
                        
                        // 목표 타일에서 캐릭터의 인덱스 찾기
                        int characterIndex = targetTile.InTilePlayerCharacters.IndexOf(character);
                        
                        // 현재 리스트의 실제 카운트를 사용하여 위치 계산
                        Vector3 targetPosition = CalculateCharacterPosition(targetTile, characterIndex);
                        targetPosition.z = character.transform.position.z;
                        
                        // 캐릭터 이동 시작
                        character.MoveTo(targetPosition);
                    }
                }
                
                // 7. 목표 타일의 아웃라인 업데이트 (캐릭터 수가 변경되었으므로)
                currentTargetTile.UpdateCharacterOutlines();
                
                // 8. 시작 타일의 아웃라인 업데이트 (스왑된 경우)
                if (shouldSwap)
                {
                    startTile.UpdateCharacterOutlines();
                }
            }
        }
        
        // 타겟 하이라이트 비활성화
        HideTargetHighlight();
        currentTargetTile = null;
        
        // 모든 타일 숨김
        HideAllTiles();
        
        // 상태 초기화 (startTile은 이동 완료 후 초기화)
        isDragging = false;
        hasSelectedCharacter = false;
        
        // 드래그 라인 숨김
        HideDragLine();
    }

    /// <summary>
    /// 캐릭터의 목표 위치를 계산 (타일의 실제 리스트 카운트 사용)
    /// </summary>
    private Vector3 CalculateCharacterPosition(Tile tile, int characterIndex)
    {
        Vector3 tileCenter = tile.transform.position;
        float xOffset = 0.1f;
        float yOffset = 0.1f;
        
        // 현재 타일의 실제 캐릭터 수 사용
        int totalCount = tile.InTilePlayerCharacters.Count;
        
        switch(totalCount)
        {
            case 1:
                return tileCenter;
            case 2:
                // 첫 번째 캐릭터는 왼쪽, 두 번째는 오른쪽
                if (characterIndex == 0)
                    return tileCenter + new Vector3(-xOffset, 0, 0);
                else
                    return tileCenter + new Vector3(xOffset, 0, 0);
            case 3:
                // 첫 번째: 왼쪽 아래, 두 번째: 오른쪽 아래, 세 번째: 위
                if (characterIndex == 0)
                    return tileCenter + new Vector3(-xOffset, -yOffset, 0);
                else if (characterIndex == 1)
                    return tileCenter + new Vector3(xOffset, -yOffset, 0);
                else
                    return tileCenter + new Vector3(0, yOffset, 0);
            default:
                return tileCenter;
        }
    }

   /// <summary>
    /// 캐릭터 이동 완료 시 호출되는 콜백
    /// </summary>
    private void OnCharacterMoveCompleted(PlayerCharacter character)
    {
        // 이동 완료된 캐릭터를 리스트에서 제거
        if (charactersToMove.Contains(character))
        {
            charactersToMove.Remove(character);
        }
        
        // 모든 캐릭터의 이동이 완료되었을 때만 최종 위치 갱신 및 상태 초기화
        if (charactersToMove.Count == 0)
        {
            // 모든 캐릭터의 이동이 완료되었으므로 최종 위치 갱신
            // (이미 타일 리스트에 추가되어 있으므로 RefreshPositionToPlayerCharacters만 호출)
            if (pendingTargetTile != null)
            {
                pendingTargetTile.RefreshPositionToPlayerCharacters();
            }
            
            // 시작 타일도 갱신 (스왑된 경우)
            if (startTile != null)
            {
                startTile.RefreshPositionToPlayerCharacters();
            }
            
            // 이동 완료된 캐릭터들을 추적 리스트에서 제거
            movingCharactersWithOutline.Clear();
            
            // 상태 초기화
            startTile = null;
            pendingTargetTile = null;
            charactersToMove.Clear();
            
            // 모든 캐릭터의 콜백 해제
            ClearCharacterCallbacks();
        }
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

        // 아웃라인은 타일의 캐릭터 수에 따라 자동으로 설정되므로 여기서는 설정하지 않음
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
        
        // 모든 캐릭터의 콜백 해제
        ClearCharacterCallbacks();
        
        SelectedCharcter = null;
        hasSelectedCharacter = false;
        isDragging = false;
        startTile = null;
        pendingTargetTile = null;
        charactersToMove.Clear();
        movingCharactersWithOutline.Clear();
        
        // 타겟 하이라이트 비활성화
        HideTargetHighlight();
        currentTargetTile = null;
        
        // 모든 타일 숨김
        HideAllTiles();
        
        // 드래그 라인 숨김
        HideDragLine();
    }

    /// <summary>
    /// 모든 캐릭터의 콜백 해제
    /// </summary>
    private void ClearCharacterCallbacks()
    {
        if (startTile != null)
        {
            foreach (var charInTile in startTile.InTilePlayerCharacters)
            {
                if (charInTile != null)
                {
                    charInTile.OnMoveCompleted = null;
                }
            }
        }
        
        if (pendingTargetTile != null)
        {
            foreach (var charInTile in pendingTargetTile.InTilePlayerCharacters)
            {
                if (charInTile != null)
                {
                    charInTile.OnMoveCompleted = null;
                }
            }
        }
    }

    /// <summary>
    /// 타일의 캐릭터 수에 따라 아웃라인 업데이트 (외부에서 호출 가능)
    /// </summary>
    public void UpdateTileCharacterOutlines(Tile tile)
    {
        if (tile == null || tile.InTilePlayerCharacters == null)
            return;

        // 타일 안에 3마리의 캐릭터가 있는 경우
        if (tile.InTilePlayerCharacters.Count == 3)
        {
            // 해당 타일의 모든 캐릭터에 아웃라인 표시
            foreach (var character in tile.InTilePlayerCharacters)
            {
                if (character != null)
                {
                    character.SetMaterial(OutlineMaterial);
                }
            }
        }
        else
        {
            // 3마리가 아닌 경우 아웃라인 제거
            // 이동 중인 캐릭터는 타일 리스트에 없으므로 여기서는 처리하지 않음
            foreach (var character in tile.InTilePlayerCharacters)
            {
                if (character != null)
                {
                    character.SetMaterial(PlayerMaterial);
                }
            }
        }
    }

    /// <summary>
    /// 모든 타일의 캐릭터 아웃라인 업데이트
    /// </summary>
    public void UpdateAllCharacterOutlines()
    {
        if (TileGroupController == null || TileGroupController.Tiles == null)
            return;

        foreach (var tile in TileGroupController.Tiles)
        {
            if (tile != null)
            {
                UpdateTileCharacterOutlines(tile);
            }
        }
    }
}
