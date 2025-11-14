using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Linq;

public class UIBottomHUD : MonoBehaviour
{
    [Header("UI References")]
    public Button SpawnCharacterButton;
    
    
    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        if (SpawnCharacterButton != null)
        {
            SpawnCharacterButton.onClick.AddListener(OnSpawnCharacterButtonClicked);
        }
    }
    
    private void OnDestroy()
    {
        // 버튼 클릭 이벤트 해제
        if (SpawnCharacterButton != null)
        {
            SpawnCharacterButton.onClick.RemoveListener(OnSpawnCharacterButtonClicked);
        }
    }
    
    /// <summary>
    /// 스폰 버튼 클릭 시 호출
    /// </summary>
    private void OnSpawnCharacterButtonClicked()
    {
        SpawnPlayerCharacter(); // 파라미터 없이 호출
    }

    public void SpawnPlayerCharacter()
    {
        // CharacterSpawnSystem 인스턴스 확인
        if (CharacterSpawnSystem.Instance == null)
        {
            Debug.LogError("[UIBottomHUD] CharacterSpawnSystem.Instance를 찾을 수 없습니다.");
            return;
        }
        
        CharacterGrades characterGrade = CharacterSpawnSystem.Instance.GetRandomCharacterGrade();
        CharacterNames characterName = CharacterSpawnSystem.Instance.GetRandomCharacterName(characterGrade);
        
        // 캐릭터 이름이 None이면 생성하지 않음
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning("[UIBottomHUD] 랜덤 캐릭터 이름을 가져올 수 없습니다.");
            return;
        }
        
        SpawnPlayerCharacter(characterName);
    }
    
  
    /// <summary>
    /// 플레이어 캐릭터 생성 (포톤 네트워크 동기화)
    /// </summary>
    public void SpawnPlayerCharacter(CharacterNames characterName)
    {
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning("[UIBottomHUD] 생성할 캐릭터 타입이 설정되지 않았습니다.");
            return;
        }
        
        // 포톤 네트워크 연결 확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("[UIBottomHUD] 포톤 네트워크에 연결되어 있지 않거나 룸에 입장하지 않았습니다.");
            return;
        }
        
        // IngameManager 확인
        if (IngameManager.Instance == null || IngameManager.Instance.TileGroupController == null)
        {
            Debug.LogError("[UIBottomHUD] IngameManager 또는 TileGroupController를 찾을 수 없습니다.");
            return;
        }
        
        string prefabName = characterName.ToString();
        
        // Resources 폴더 기준 상대 경로 (PhotonNetwork.Instantiate에서 사용)
        string prefabPath = "PlayerCharacters/SPUM_" + prefabName;
        
        // 로컬에서 프리팹 존재 확인
        GameObject characterPrefab = Resources.Load<GameObject>(prefabPath);
        
        if (characterPrefab == null)
        {
            Debug.LogError($"[UIBottomHUD] Resources 폴더에서 '{prefabPath}' 프리팹을 찾을 수 없습니다.");
            return;
        }
        
        // 스폰 위치 계산 (일단 임시 위치)
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;
        
        // 포톤 네트워크를 통해 캐릭터 생성
        // Resources 폴더 기준 상대 경로를 전달해야 함
        GameObject characterInstance = PhotonNetwork.Instantiate(
            prefabPath, // 전체 경로 전달 (Resources 폴더 기준)
            spawnPosition,
            spawnRotation
        );
        
        if (characterInstance != null)
        {
            // PlayerCharacter 컴포넌트 확인
            PlayerCharacter playerCharacter = characterInstance.GetComponent<PlayerCharacter>();
            if (playerCharacter != null)
            {
                // 캐릭터 이름 설정
                playerCharacter.Name = characterName;
                
                // PlayerCharacterGroup처럼 타일 찾기 및 연결
                Tile nextSpawnTile = IngameManager.Instance.TileGroupController.GetNextSpawnTile(playerCharacter);
                if (nextSpawnTile != null)
                {
                    // 타일에 캐릭터 배치
                    nextSpawnTile.SetInTilePlayerCharacters(playerCharacter);
                }
                else
                {
                    Debug.LogWarning("[UIBottomHUD] 스폰할 수 있는 타일이 없습니다.");
                    PhotonNetwork.Destroy(characterInstance);
                }
            }
            else
            {
                Debug.LogError($"[UIBottomHUD] '{prefabName}' 프리팹에 PlayerCharacter 컴포넌트가 없습니다.");
                PhotonNetwork.Destroy(characterInstance);
            }
        }
    }
}
