using UnityEngine;

public class TileGroupController : MonoBehaviour
{

    public Tile[] Tiles;

    public Tile TargetTile;

    public GameObject TargetHighlightObject;



   public void ShowTargetTile(Vector3 position)
   {
     TargetHighlightObject.transform.position = position;
     TargetHighlightObject.SetActive(true);
   }

   public void HideTargetTile()
   {
    TargetHighlightObject.SetActive(false);
   }

   public Tile GetNextSpawnTile(PlayerCharacter character)
   {
    for(int i = 0; i < Tiles.Length; i++)
    {
            if(Tiles[i].IsEmpty)
            {
                return Tiles[i];
            }
            else if(Tiles[i].InTilePlayerCharacters.Count>1&&Tiles[i].InTilePlayerCharacters.Count<3)
            {
                if(Tiles[i].InTilePlayerCharacters[0].Name == character.Name)
                {
                    return Tiles[i];
                }
            }
            
    }
    return null;

   }

}
