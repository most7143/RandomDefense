using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    public SpriteRenderer Renderer;

    public int Index;


    public bool IsEmpty = true;

    public List<PlayerCharacter> InTilePlayerCharacters = new List<PlayerCharacter>(); // 초기화 추가

    public void SetIndex(int index)
    {
        Index = index;
    }


    public void RefreshPositionToPlayerCharacters()
    {
        Vector3 tileCenter = transform.position;

        switch(InTilePlayerCharacters.Count)
        {
            case 1:
                InTilePlayerCharacters[0].transform.position = tileCenter;
                break;
            case 2:
                InTilePlayerCharacters[0].transform.position = tileCenter + new Vector3(-0.5f, 0, 0);
                InTilePlayerCharacters[1].transform.position = tileCenter + new Vector3(0.5f, 0, 0);
                break;
            case 3:
                InTilePlayerCharacters[0].transform.position = tileCenter + new Vector3(0, 0.5f, 0);
                InTilePlayerCharacters[1].transform.position = tileCenter + new Vector3(-0.5f, -0.5f, 0);
                InTilePlayerCharacters[2].transform.position = tileCenter + new Vector3(0.5f, -0.5f, 0);
                break;
        }
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
        InTilePlayerCharacters.Add(playerCharacter);
        RefreshPositionToPlayerCharacters();
    }

    public void ClearInTilePlayerCharacters()
    {
        InTilePlayerCharacters.Clear();
    }

}
