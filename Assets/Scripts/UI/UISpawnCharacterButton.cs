using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Linq;


public class UISpawnCharacterButton : MonoBehaviour
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
            Debug.LogError("[UISpawnCharacterButton] CharacterSpawnSystem.Instance를 찾을 수 없습니다.");
            return;
        }
        
        CharacterGrades characterGrade = CharacterSpawnSystem.Instance.GetRandomCharacterGrade();
        CharacterNames characterName = CharacterSpawnSystem.Instance.GetRandomCharacterName(characterGrade);
        
        // 캐릭터 이름이 None이면 생성하지 않음
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning("[UISpawnCharacterButton] 랜덤 캐릭터 이름을 가져올 수 없습니다.");
            return;
        }
        
        SpawnPlayerCharacter(characterName);
    }
    
  
    /// <summary>
    /// 플레이어 캐릭터 생성 (PlayerCharacterGroup 풀링 시스템 사용)
    /// </summary>
    public void SpawnPlayerCharacter(CharacterNames characterName)
    {
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning("[UISpawnCharacterButton] 생성할 캐릭터 타입이 설정되지 않았습니다.");
            return;
        }
        
        // 포톤 네트워크 연결 확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("[UISpawnCharacterButton] 포톤 네트워크에 연결되어 있지 않거나 룸에 입장하지 않았습니다.");
            return;
        }
        
        // IngameManager 확인
        if (IngameManager.Instance == null || IngameManager.Instance.TileGroupController == null)
        {
            Debug.LogError("[UISpawnCharacterButton] IngameManager 또는 TileGroupController를 찾을 수 없습니다.");
            return;
        }
        
        // PlayerCharacterGroup 싱글톤 확인
        if (PlayerCharacterGroup.Instance == null)
        {
            Debug.LogError("[UISpawnCharacterButton] PlayerCharacterGroup.Instance를 찾을 수 없습니다.");
            return;
        }
        
        // PlayerCharacterGroup에서 캐릭터 소환 (풀에서 가져오기)
        PlayerCharacter playerCharacter = PlayerCharacterGroup.Instance.SpawnCharacter(characterName);
        
        if (playerCharacter == null)
        {
            Debug.LogWarning($"[UISpawnCharacterButton] '{characterName}' 캐릭터를 소환할 수 없습니다. (풀이 가득 찼거나 초기화되지 않았을 수 있음)");
            return;
        }
        
        PhotonView characterPV = playerCharacter.GetComponent<PhotonView>();
        
        // PhotonView 네트워크 등록 (풀에서 가져온 캐릭터는 ViewID가 0일 수 있음)
        if (characterPV != null && PhotonNetwork.IsConnected)
        {
            // PhotonView가 네트워크에 등록되지 않았다면 등록
            if (characterPV.ViewID == 0)
            {
                // ViewID 할당 및 네트워크에 등록
                characterPV.ViewID = PhotonNetwork.AllocateViewID(PhotonNetwork.LocalPlayer.ActorNumber);
                PhotonNetwork.RegisterPhotonView(characterPV);
            }
        }
        
        // 타일 배치
        Tile nextSpawnTile = IngameManager.Instance.TileGroupController.GetNextSpawnTile(playerCharacter);
        if (nextSpawnTile != null)
        {
            // 소환 위치 계산 (캐릭터 추가 전에 계산)
            int characterIndex = nextSpawnTile.InTilePlayerCharacters.Count; // 추가될 인덱스
            int totalCount = nextSpawnTile.InTilePlayerCharacters.Count + 1; // 추가 후 총 개수
            Vector3 spawnPosition = nextSpawnTile.CalculateCharacterPosition(characterIndex, totalCount);
            
            // 타일에 캐릭터 배치 (이 함수 내부에서 RefreshPositionToPlayerCharacters가 호출됨)
            nextSpawnTile.SetInTilePlayerCharacters(playerCharacter);
            
            // 네트워크 동기화: 모든 클라이언트에 소환 위치 전달
            if (characterPV != null && characterPV.IsMine)
            {
                characterPV.RPC("SetSpawnPosition", RpcTarget.All, spawnPosition);
            }
            else if (characterPV == null)
            {
                // PhotonView가 없는 경우 로컬에서만 설정
                playerCharacter.transform.position = spawnPosition;
            }
        }
        else
        {
            Debug.LogWarning("[UISpawnCharacterButton] 스폰할 수 있는 타일이 없습니다.");
            // 타일이 없으면 캐릭터 비활성화 (풀로 반환)
            PlayerCharacterGroup.Instance.DespawnCharacter(playerCharacter);
        }
    }
}
