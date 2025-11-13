using UnityEngine;
using System.Collections.Generic;

public class PlayerCharacterGroup : MonoBehaviour
{
    public List<PlayerCharacter> Characters = new List<PlayerCharacter>();



private void Start()
{
    if(Characters.Count > 0)
    {
        for(int i = 0; i < Characters.Count; i++)
        {
           

            Tile nextSpawnTile = IngameManager.Instance.TileGroupController.GetNextSpawnTile(Characters[i]);
            if(nextSpawnTile != null)
            {
                nextSpawnTile.SetInTilePlayerCharacters(Characters[i]);
            }
        }
        
    }
}
}

