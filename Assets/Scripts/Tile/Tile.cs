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
    /// 캐릭터 위치 계산 (공통 함수)
    /// </summary>
    /// <param name="characterIndex">캐릭터의 인덱스 (0, 1, 2)</param>
    /// <param name="totalCharacterCount">총 캐릭터 수</param>
    /// <returns>계산된 위치</returns>
    public Vector3 CalculateCharacterPosition(int characterIndex, int totalCharacterCount)
    {
        Vector3 tileCenter = transform.position;
        float xOffset = 0.1f;
        float yOffset = 0.1f;
        
        switch(totalCharacterCount)
        {
            case 1:
                return new Vector3(tileCenter.x, tileCenter.y, tileCenter.z);
            case 2:
                // 첫 번째 캐릭터는 왼쪽, 두 번째는 오른쪽 (수평 정렬)
                if (characterIndex == 0)
                    return new Vector3(tileCenter.x - xOffset, tileCenter.y, tileCenter.z);
                else
                    return new Vector3(tileCenter.x + xOffset, tileCenter.y, tileCenter.z);
            case 3:
                // 첫 번째: 왼쪽 아래, 두 번째: 오른쪽 아래, 세 번째: 위
                if (characterIndex == 0)
                    return new Vector3(tileCenter.x - xOffset, tileCenter.y - yOffset, tileCenter.z);
                else if (characterIndex == 1)
                    return new Vector3(tileCenter.x + xOffset, tileCenter.y - yOffset, tileCenter.z);
                else
                    return new Vector3(tileCenter.x, tileCenter.y + yOffset, tileCenter.z);
            default:
                return tileCenter;
        }
    }

    /// <summary>
    /// 캐릭터가 추가될 때의 목표 위치를 계산 (이동 전에 사용)
    /// </summary>
    public Vector3 GetTargetPositionForCharacter(int characterIndex)
    {
        // 현재 타일의 캐릭터 수 + 추가될 캐릭터 수를 고려
        int totalCount = InTilePlayerCharacters.Count + 1; // 추가될 캐릭터 포함
        return CalculateCharacterPosition(characterIndex, totalCount);
    }

    public void RefreshPositionToPlayerCharacters()
    {
        int characterCount = InTilePlayerCharacters.Count;
        
        for (int i = 0; i < characterCount; i++)
        {
            if (InTilePlayerCharacters[i] != null && !InTilePlayerCharacters[i].IsMoving())
            {
                Vector3 position = CalculateCharacterPosition(i, characterCount);
                InTilePlayerCharacters[i].transform.position = position;
            }
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

        IsEmpty = false;
        
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
        
        // 리스트가 비어있으면 IsEmpty를 true로 설정
        // 하지만 리스트에 캐릭터가 있으면 false로 설정
        if (InTilePlayerCharacters.Count == 0)
        {
            IsEmpty = true;
        }
        else
        {
            IsEmpty = false; // 캐릭터가 있으면 비어있지 않음
        }
        
        // 위치 갱신 (아웃라인은 RefreshPositionToPlayerCharacters에서 처리)
        RefreshPositionToPlayerCharacters();
    }
}

    public void ClearInTilePlayerCharacters()
    {
        InTilePlayerCharacters.Clear();
        IsEmpty = true;
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
