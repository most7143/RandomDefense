using UnityEngine;

public class TileGroupController : MonoBehaviour
{

    public Tile[] Tiles;

    public Tile TargetTile;

    public GameObject TargetHighlightObject;

    public GameObject AttackRangeHighlightObject;



   public void ShowTargetTile(Vector3 position)
   {
     TargetHighlightObject.transform.position = position;
     TargetHighlightObject.SetActive(true);
   }

   public void HideTargetTile()
   {
    TargetHighlightObject.SetActive(false);
   }

    /// <summary>
    /// 공격 범위 하이라이트 표시 (타일 중심 위치에)
    /// </summary>
    public void ShowAttackRangeHighlight(Vector3 position, float attackRange)
    {
        if (AttackRangeHighlightObject != null)
        {
            AttackRangeHighlightObject.transform.position = position;
            
            // 공격 범위에 맞게 크기 조정 (AttackRangeHighlightObject가 SpriteRenderer를 가지고 있다고 가정)
            SpriteRenderer spriteRenderer = AttackRangeHighlightObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // 스프라이트의 기본 크기 기준으로 공격 범위에 맞게 스케일 조정
                // 원형 범위라면 반지름 * 2가 전체 크기가 되어야 함
                float scale = (attackRange * 2f) / spriteRenderer.sprite.bounds.size.x;
                AttackRangeHighlightObject.transform.localScale = Vector3.one * scale;
            }
            
            AttackRangeHighlightObject.SetActive(true);
        }
    }

    /// <summary>
    /// 공격 범위 하이라이트 숨김
    /// </summary>
    public void HideAttackRangeHighlight()
    {
        if (AttackRangeHighlightObject != null)
        {
            AttackRangeHighlightObject.SetActive(false);
        }
    }

  public Tile GetNextSpawnTile(PlayerCharacter character)
   {
        // 1단계: 같은 이름의 캐릭터가 이미 배치된 타일 찾기
        // 모든 타일을 탐색하여 같은 이름의 캐릭터가 있는 타일 중 3개 미만인 타일 찾기
        for(int i = 0; i < Tiles.Length; i++)
        {
            if(Tiles[i].InTilePlayerCharacters != null && Tiles[i].InTilePlayerCharacters.Count > 0)
            {
                // 타일에 캐릭터가 있고 3개 미만인 경우
                if(Tiles[i].InTilePlayerCharacters.Count < 3)
                {
                    // 같은 이름의 캐릭터가 있는지 확인 (모든 캐릭터 확인)
                    foreach(var charInTile in Tiles[i].InTilePlayerCharacters)
                    {
                        if(charInTile != null && charInTile.Name == character.Name)
                        {
                            Debug.Log($"같은 이름의 캐릭터 발견: {character.Name} - 타일 인덱스: {Tiles[i].Index}");
                            return Tiles[i];
                        }
                    }
                }
            }
        }
        
        // 2단계: 같은 이름의 캐릭터가 없으면, 빈 타일 중 인덱스가 가장 낮은 타일 찾기
        Tile emptyTileWithLowestIndex = null;
        int lowestIndex = int.MaxValue;
        
        for(int i = 0; i < Tiles.Length; i++)
        {
            if(Tiles[i].IsEmpty)
            {
                // 인덱스가 더 낮은 빈 타일을 찾으면 업데이트
                if(Tiles[i].Index < lowestIndex)
                {
                    lowestIndex = Tiles[i].Index;
                    emptyTileWithLowestIndex = Tiles[i];
                }
            }
        }
        
        if(emptyTileWithLowestIndex != null)
        {
            Debug.Log($"빈 타일 발견: 인덱스 {emptyTileWithLowestIndex.Index}");
            return emptyTileWithLowestIndex;
        }
        
        // 빈 타일도 없으면 null 반환
        Debug.LogWarning("배치할 수 있는 타일이 없습니다.");
        return null;
   }

}
