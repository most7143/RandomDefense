using UnityEngine;
using System.Collections.Generic;
using System;
using Photon.Pun;
using System.Collections;

public class PlayerCharacterGroup : MonoBehaviour
{
    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static PlayerCharacterGroup Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("각 캐릭터 종류당 미리 생성할 개수")]
    public int PoolSizePerCharacter = 20;

    public Dictionary<CharacterNames, List<PlayerCharacter>> Characters = new Dictionary<CharacterNames, List<PlayerCharacter>>();

    public int MaxAliveCharacterCount = 20;

    public int CurrentAliveCharacterCount = 0;

    /// <summary>
    /// 초기화 여부 (중복 초기화 방지)
    /// </summary>
    private bool isInitialized = false;

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            // 씬 전환 시에도 유지 (로딩 씬에서 초기화 후 게임 씬에서도 사용)
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 씬 로딩 시 모든 캐릭터 프리팹을 미리 동적 생성하고 비활성화 (코루틴 버전)
    /// </summary>
    public IEnumerator InitializeCoroutine()
    {
        // 이미 초기화되었으면 중복 초기화 방지
        if (isInitialized)
        {
            Debug.LogWarning("[PlayerCharacterGroup] 이미 초기화되었습니다!");
            yield break;
        }

        // CharacterNames enum의 모든 값 가져오기
        CharacterNames[] GetAllCharacterNames = Enum.GetValues(typeof(CharacterNames)) as CharacterNames[];

        int totalCharacters = GetAllCharacterNames.Length - 1; // None 제외
        int processedCount = 0;

        // None을 제외한 모든 캐릭터 프리팹 미리 생성
        for (int i = 1; i < GetAllCharacterNames.Length; i++)
        {
            CharacterNames characterName = GetAllCharacterNames[i];
            
            // CharacterNames를 문자열로 변환하여 프리팹 이름 생성
            string prefabName = characterName.ToString();

            // Resources 폴더에서 프리팹 로드
            GameObject characterPrefab = Resources.Load<GameObject>("PlayerCharacters/" + "SPUM_" + prefabName);

            if (characterPrefab == null)
            {
                Debug.LogWarning($"[PlayerCharacterGroup] Resources 폴더에서 '{prefabName}' 프리팹을 찾을 수 없습니다!");
                processedCount++;
                continue;
            }

            // 각 캐릭터 종류마다 List 초기화
            List<PlayerCharacter> characterList = new List<PlayerCharacter>();
            Characters[characterName] = characterList;

            // 각 캐릭터 종류당 PoolSizePerCharacter개씩 생성
            for (int j = 0; j < PoolSizePerCharacter; j++)
            {
                // 포톤 네트워크가 연결되어 있으면 PhotonNetwork.Instantiate 사용 (네트워크 동기화)
                // 연결되어 있지 않으면 일반 Instantiate 사용
                GameObject characterInstance;
                
                // 생성 위치를 카메라 밖으로 설정 (보이지 않게)
                Vector3 hiddenPosition = new Vector3(0, -1000, 0);
                
                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
                {
                    // PhotonNetwork.Instantiate 사용 (네트워크 동기화를 위해)
                    string prefabPath = "PlayerCharacters/SPUM_" + prefabName; // Resources 폴더 기준 경로
                    characterInstance = PhotonNetwork.Instantiate(
                        prefabPath,
                        hiddenPosition,
                        Quaternion.identity,
                        0,
                        null
                    );
                }
                else
                {
                    // 일반 Instantiate 사용 (네트워크 없을 때)
                    characterInstance = Instantiate(characterPrefab, hiddenPosition, Quaternion.identity);
                }
                
                if (characterInstance == null)
                {
                    Debug.LogError($"[PlayerCharacterGroup] '{prefabName}' 프리팹 생성 실패!");
                    continue;
                }
                
                // 생성 직후 즉시 비활성화 (로딩 씬에서 보이지 않게)
                characterInstance.SetActive(false);
                
                // 부모를 현재 클래스(PlayerCharacterGroup)로 설정
                characterInstance.transform.SetParent(transform);
                
                characterInstance.name = $"{prefabName}_{j}"; // 이름 설정 (인덱스 포함)
                
                // PlayerCharacter 컴포넌트 가져오기
                PlayerCharacter playerCharacter = characterInstance.GetComponent<PlayerCharacter>();
                
                if (playerCharacter == null)
                {
                    Debug.LogWarning($"[PlayerCharacterGroup] '{prefabName}' 프리팹에 PlayerCharacter 컴포넌트가 없습니다!");
                    if (PhotonNetwork.IsConnected)
                    {
                        PhotonNetwork.Destroy(characterInstance);
                    }
                    else
                    {
                        Destroy(characterInstance);
                    }
                    continue;
                }

                // 캐릭터 이름 설정
                playerCharacter.Name = characterName;

                // 씬 전환 시에도 파괴되지 않도록 설정
                DontDestroyOnLoad(characterInstance);

                // List에 추가
                characterList.Add(playerCharacter);

                // 여러 개 생성 시 프레임 분산 (성능 개선)
                if (j % 5 == 0)
                {
                    yield return null; // 매 5개마다 한 프레임 대기
                }
            }

            processedCount++;
            Debug.Log($"[PlayerCharacterGroup] 캐릭터 프리팹 미리 생성: {characterName} x{PoolSizePerCharacter}개 ({processedCount}/{totalCharacters})");
            
            // 각 캐릭터 종류 생성 후 한 프레임 대기
            yield return null;
        }

        int totalCount = 0;
        foreach (var list in Characters.Values)
        {
            totalCount += list.Count;
        }
        
        isInitialized = true;
        Debug.Log($"[PlayerCharacterGroup] 초기화 완료: {Characters.Count}개 캐릭터 종류, 총 {totalCount}개 프리팹 생성됨");
    }

    /// <summary>
    /// 씬 로딩 시 모든 캐릭터 프리팹을 미리 동적 생성하고 비활성화 (동기 버전)
    /// </summary>
    public void Initialize()
    {
        // 이미 초기화되었으면 중복 초기화 방지
        if (isInitialized)
        {
            Debug.LogWarning("[PlayerCharacterGroup] 이미 초기화되었습니다!");
            return;
        }

        // CharacterNames enum의 모든 값 가져오기
        CharacterNames[] GetAllCharacterNames = Enum.GetValues(typeof(CharacterNames)) as CharacterNames[];

        // None을 제외한 모든 캐릭터 프리팹 미리 생성
        for (int i = 1; i < GetAllCharacterNames.Length; i++)
        {
            CharacterNames characterName = GetAllCharacterNames[i];
            
            // CharacterNames를 문자열로 변환하여 프리팹 이름 생성
            string prefabName = characterName.ToString();

            // Resources 폴더에서 프리팹 로드
            GameObject characterPrefab = Resources.Load<GameObject>("PlayerCharacters/" + "SPUM_"+prefabName);

            if (characterPrefab == null)
            {
                Debug.LogWarning($"[PlayerCharacterGroup] Resources 폴더에서 '{prefabName}' 프리팹을 찾을 수 없습니다!");
                continue;
            }

            // 각 캐릭터 종류마다 List 초기화
            List<PlayerCharacter> characterList = new List<PlayerCharacter>();
            Characters[characterName] = characterList;

            // 각 캐릭터 종류당 PoolSizePerCharacter개씩 생성
            for (int j = 0; j < PoolSizePerCharacter; j++)
            {
                // 포톤 네트워크가 연결되어 있으면 PhotonNetwork.Instantiate 사용 (네트워크 동기화)
                // 연결되어 있지 않으면 일반 Instantiate 사용
                GameObject characterInstance;
                
                // 생성 위치를 카메라 밖으로 설정 (보이지 않게)
                Vector3 hiddenPosition = new Vector3(0, -1000, 0);
                
                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
                {
                    // PhotonNetwork.Instantiate 사용 (네트워크 동기화를 위해)
                    string prefabPath = "PlayerCharacters/SPUM_" + prefabName; // Resources 폴더 기준 경로
                    characterInstance = PhotonNetwork.Instantiate(
                        prefabPath,
                        hiddenPosition,
                        Quaternion.identity,
                        0,
                        null
                    );
                }
                else
                {
                    // 일반 Instantiate 사용 (네트워크 없을 때)
                    characterInstance = Instantiate(characterPrefab, hiddenPosition, Quaternion.identity);
                }
                
                if (characterInstance == null)
                {
                    Debug.LogError($"[PlayerCharacterGroup] '{prefabName}' 프리팹 생성 실패!");
                    continue;
                }
                
                // 생성 직후 즉시 비활성화 (로딩 씬에서 보이지 않게)
                characterInstance.SetActive(false);
                
                // 부모를 현재 클래스(PlayerCharacterGroup)로 설정
                characterInstance.transform.SetParent(transform);
                
                characterInstance.name = $"{prefabName}_{j}"; // 이름 설정 (인덱스 포함)
                
                // PlayerCharacter 컴포넌트 가져오기
                PlayerCharacter playerCharacter = characterInstance.GetComponent<PlayerCharacter>();
                
                if (playerCharacter == null)
                {
                    Debug.LogWarning($"[PlayerCharacterGroup] '{prefabName}' 프리팹에 PlayerCharacter 컴포넌트가 없습니다!");
                    if (PhotonNetwork.IsConnected)
                    {
                        PhotonNetwork.Destroy(characterInstance);
                    }
                    else
                    {
                        Destroy(characterInstance);
                    }
                    continue;
                }

                // 캐릭터 이름 설정
                playerCharacter.Name = characterName;

                // 씬 전환 시에도 파괴되지 않도록 설정
                DontDestroyOnLoad(characterInstance);

                // List에 추가
                characterList.Add(playerCharacter);
            }

            Debug.Log($"[PlayerCharacterGroup] 캐릭터 프리팹 미리 생성: {characterName} x{PoolSizePerCharacter}개");
        }

        int totalCount = 0;
        foreach (var list in Characters.Values)
        {
            totalCount += list.Count;
        }
        
        isInitialized = true;
        Debug.Log($"[PlayerCharacterGroup] 초기화 완료: {Characters.Count}개 캐릭터 종류, 총 {totalCount}개 프리팹 생성됨");
    }

    /// <summary>
    /// 캐릭터 소환 (SetActive를 통해 활성화, 네트워크 동기화)
    /// </summary>
    /// <param name="characterName">소환할 캐릭터 이름</param>
    /// <returns>소환된 PlayerCharacter (없으면 null)</returns>
    public PlayerCharacter SpawnCharacter(CharacterNames characterName)
    {
        // None이면 소환하지 않음
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] None 캐릭터는 소환할 수 없습니다!");
            return null;
        }

        // Dictionary에서 캐릭터 List 찾기
        if (!Characters.ContainsKey(characterName))
        {
            Debug.LogError($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 초기화되지 않았습니다!");
            return null;
        }

        List<PlayerCharacter> characterList = Characters[characterName];

        if (characterList == null || characterList.Count == 0)
        {
            Debug.LogError($"[PlayerCharacterGroup] '{characterName}' 캐릭터 리스트가 비어있습니다!");
            return null;
        }

        // 비활성화된 캐릭터 찾기
        PlayerCharacter character = null;
        foreach (PlayerCharacter charInList in characterList)
        {
            if (charInList != null && !charInList.gameObject.activeSelf)
            {
                character = charInList;
                break;
            }
        }

        // 사용 가능한 캐릭터가 없으면 경고
        if (character == null)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터 풀이 모두 사용 중입니다! (풀 크기: {characterList.Count})");
            return null;
        }

        // PhotonView 네트워크 등록 확인 및 처리
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonView characterPV = character.GetComponent<PhotonView>();
            
            if (characterPV != null)
            {
                // PhotonView가 네트워크에 등록되지 않았다면 등록
                if (characterPV.ViewID == 0)
                {
                    // ViewID 할당 및 네트워크에 등록
                    characterPV.ViewID = PhotonNetwork.AllocateViewID(PhotonNetwork.LocalPlayer.ActorNumber);
                    PhotonNetwork.RegisterPhotonView(characterPV);
                }
                
                // 네트워크에서 소유권 확인 (필요시)
                if (PhotonNetwork.IsMasterClient && !characterPV.IsMine)
                {
                    // 마스터 클라이언트가 소유권을 가지도록 설정
                    characterPV.TransferOwnership(PhotonNetwork.LocalPlayer);
                }
            }
        }

        // 캐릭터 활성화 (네트워크 동기화를 위해 RPC로 처리할 수도 있음)
        character.gameObject.SetActive(true);
        
        // 생존 캐릭터 수 증가
        CurrentAliveCharacterCount++;

        Debug.Log($"[PlayerCharacterGroup] 캐릭터 소환: {characterName} (현재 생존: {CurrentAliveCharacterCount}/{MaxAliveCharacterCount})");
        return character;
    }

    /// <summary>
    /// 캐릭터 제거 (SetActive를 통해 비활성화)
    /// </summary>
    /// <param name="character">제거할 PlayerCharacter 인스턴스</param>
    public void DespawnCharacter(PlayerCharacter character)
    {
        if (character == null)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] 제거할 캐릭터가 null입니다!");
            return;
        }

        CharacterNames characterName = character.Name;

        // None이면 제거하지 않음
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] None 캐릭터는 제거할 수 없습니다!");
            return;
        }

        // Dictionary에서 캐릭터 List 찾기
        if (!Characters.ContainsKey(characterName))
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 초기화되지 않았습니다!");
            return;
        }

        List<PlayerCharacter> characterList = Characters[characterName];

        // List에 해당 캐릭터가 있는지 확인
        if (!characterList.Contains(character))
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 이 그룹에 속하지 않습니다!");
            return;
        }

        // 이미 비활성화되어 있으면 그대로 반환
        if (!character.gameObject.activeSelf)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 이미 비활성화되어 있습니다!");
            return;
        }

        // PhotonView 처리 (네트워크 동기화)
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonView characterPV = character.GetComponent<PhotonView>();
            
            // PhotonView가 있다면 네트워크에서 비활성화 알림
            // 주의: PhotonNetwork.Destroy를 사용하지 않음 (풀링을 위해)
            // 대신 모든 클라이언트에서 SetActive(false)를 호출하도록 RPC 사용 가능
        }

        // 캐릭터 비활성화
        character.gameObject.SetActive(false);
        
        // 생존 캐릭터 수 감소
        CurrentAliveCharacterCount--;
        if (CurrentAliveCharacterCount < 0)
        {
            CurrentAliveCharacterCount = 0; // 음수 방지
        }

        Debug.Log($"[PlayerCharacterGroup] 캐릭터 제거: {characterName} (현재 생존: {CurrentAliveCharacterCount}/{MaxAliveCharacterCount})");
    }

    /// <summary>
    /// 캐릭터 제거 (CharacterNames로 제거)
    /// </summary>
    /// <param name="characterName">제거할 캐릭터 이름</param>
    public void DespawnCharacter(CharacterNames characterName)
    {
        // None이면 제거하지 않음
        if (characterName == CharacterNames.None)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] None 캐릭터는 제거할 수 없습니다!");
            return;
        }

        // Dictionary에서 캐릭터 List 찾기
        if (!Characters.ContainsKey(characterName))
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 초기화되지 않았습니다!");
            return;
        }

        List<PlayerCharacter> characterList = Characters[characterName];

        // 활성화된 캐릭터 찾기
        PlayerCharacter character = null;
        foreach (PlayerCharacter charInList in characterList)
        {
            if (charInList != null && charInList.gameObject.activeSelf)
            {
                character = charInList;
                break;
            }
        }

        // 활성화된 캐릭터가 없으면 경고
        if (character == null)
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 활성화된 캐릭터가 없습니다!");
            return;
        }

        // 캐릭터 제거 (위의 DespawnCharacter 오버로드 사용)
        DespawnCharacter(character);
    }
}

