using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    public SpriteRenderer Renderer;

    public int Index;


    public bool IsEmpty = true;

    public List<PlayerCharacter> InTilePlayerCharacters = new List<PlayerCharacter>();

    public void SetIndex(int index)
    {
        Index = index;
    }

    /// <summary>
    /// 캐릭터가 추가될 때의 목표 위치를 계산 (이동 전에 사용)
    /// </summary>
    public Vector3 GetTargetPositionForCharacter(int characterIndex)
    {
        Vector3 tileCenter = transform.position;
        float xOffset = 0.1f;
        float yOffset = 0.1f;
        
        // 현재 타일의 캐릭터 수 + 추가될 캐릭터 수를 고려
        int totalCount = InTilePlayerCharacters.Count + 1; // 추가될 캐릭터 포함
        
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

    public void RefreshPositionToPlayerCharacters()
    {
        Vector3 tileCenter = transform.position;

        float xOffset = 0.1f;
        float yOffset = 0.1f;
        
        switch(InTilePlayerCharacters.Count)
        {
            case 1:
                // 이동 중이 아닌 경우에만 위치 설정
                if (InTilePlayerCharacters[0] != null && !InTilePlayerCharacters[0].IsMoving())
                {
                    InTilePlayerCharacters[0].transform.position = tileCenter;
                }
                break;
            case 2:
                // 첫 번째 캐릭터 위치 설정 (이동 중이 아닌 경우에만)
                if (InTilePlayerCharacters[0] != null && !InTilePlayerCharacters[0].IsMoving())
                {
                    InTilePlayerCharacters[0].transform.position = tileCenter + new Vector3(-xOffset, 0, 0);
                }
                // 두 번째 캐릭터 위치 설정 (이동 중이 아닌 경우에만)
                if (InTilePlayerCharacters[1] != null && !InTilePlayerCharacters[1].IsMoving())
                {
                    InTilePlayerCharacters[1].transform.position = tileCenter + new Vector3(xOffset, 0, 0);
                }
                break;
            case 3:
                // 첫 번째 캐릭터 위치 설정 (이동 중이 아닌 경우에만)
                if (InTilePlayerCharacters[0] != null && !InTilePlayerCharacters[0].IsMoving())
                {
                      InTilePlayerCharacters[0].transform.position = tileCenter + new Vector3(-xOffset, -yOffset, 0);
                      
                }
                // 두 번째 캐릭터 위치 설정 (이동 중이 아닌 경우에만)
                if (InTilePlayerCharacters[1] != null && !InTilePlayerCharacters[1].IsMoving())
                {
                    InTilePlayerCharacters[1].transform.position = tileCenter + new Vector3(xOffset, -yOffset, 0);
                }
                // 세 번째 캐릭터 위치 설정 (이동 중이 아닌 경우에만)
                if (InTilePlayerCharacters[2] != null && !InTilePlayerCharacters[2].IsMoving())
                {
                  
                    InTilePlayerCharacters[2].transform.position = tileCenter + new Vector3(0, yOffset, 0);
                }
                break;
        }
        
        // 위치 갱신 후 아웃라인 업데이트 (항상 실행)
        UpdateCharacterOutlines();
    }

    /// <summary>
    /// 타겟 렌더러 활성화
    /// </summary>
    public void ShowTargetRenderer()
    {
        if (Renderer != null)
        {
            Renderer.enabled = true;
        }
    }

    /// <summary>
    /// 타겟 렌더러 비활성화
    /// </summary>
    public void HideTargetRenderer()
    {
        if (Renderer != null)
        {
            Renderer.enabled = false;
        }
    }


    public void SetInTilePlayerCharacters(PlayerCharacter playerCharacter)
    {
        // 이미 리스트에 있으면 추가하지 않음
        if (InTilePlayerCharacters.Contains(playerCharacter))
        {
            return;
        }
        
        InTilePlayerCharacters.Add(playerCharacter);
        
        // 위치 갱신 (이동 중이 아닌 경우에만 위치 설정, 아웃라인은 항상 업데이트)
        RefreshPositionToPlayerCharacters();
    }

    /// <summary>
    /// 타일에서 캐릭터 제거
    /// </summary>
    public void RemoveInTilePlayerCharacter(PlayerCharacter playerCharacter)
    {
        if (InTilePlayerCharacters != null && InTilePlayerCharacters.Contains(playerCharacter))
        {
            InTilePlayerCharacters.Remove(playerCharacter);
            // 위치 갱신 (아웃라인은 RefreshPositionToPlayerCharacters에서 처리)
            RefreshPositionToPlayerCharacters();
        }
    }

    public void ClearInTilePlayerCharacters()
    {
        InTilePlayerCharacters.Clear();
    }

    /// <summary>
    /// 타일의 캐릭터 수에 따라 아웃라인 업데이트
    /// </summary>
    public void UpdateCharacterOutlines()
    {
        // PlayerController 찾기
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            // 이 타일의 아웃라인만 업데이트
            playerController.UpdateTileCharacterOutlines(this);
        }
    }
}
