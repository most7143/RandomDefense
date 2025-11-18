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
                // 로딩 씬에서는 항상 일반 Instantiate 사용
                // (네트워크 동기화는 실제 소환 시에만 처리)
                // 이렇게 하면 씬 전환 시 PhotonView 동기화 문제를 방지할 수 있습니다.
                GameObject characterInstance;

                // 생성 위치를 카메라 밖으로 설정 (보이지 않게)
                Vector3 hiddenPosition = new Vector3(0, -1000, 0);

                // 일반 Instantiate 사용 (로딩 씬에서는 네트워크 동기화 없이 준비만)
                characterInstance = Instantiate(characterPrefab, hiddenPosition, Quaternion.identity);

                if (characterInstance == null)
                {
                    Debug.LogError($"[PlayerCharacterGroup] '{prefabName}' 프리팹 생성 실패!");
                    continue;
                }

                // 부모를 현재 클래스(PlayerCharacterGroup)로 설정
                characterInstance.transform.SetParent(transform);

                characterInstance.name = $"{prefabName}_{j}"; // 이름 설정 (인덱스 포함)

                // PlayerCharacter 컴포넌트 가져오기
                PlayerCharacter playerCharacter = characterInstance.GetComponent<PlayerCharacter>();

                if (playerCharacter == null)
                {
                    Debug.LogWarning($"[PlayerCharacterGroup] '{prefabName}' 프리팹에 PlayerCharacter 컴포넌트가 없습니다!");
                    Destroy(characterInstance);
                    continue;
                }

                // PhotonView가 있으면 ViewID를 0으로 초기화 (아직 네트워크에 등록하지 않음)
                PhotonView pv = characterInstance.GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.ViewID = 0; // 나중에 실제 소환 시 네트워크에 등록
                }

                // 캐릭터 이름 설정
                playerCharacter.Name = characterName;

                // List에 추가
                characterList.Add(playerCharacter);

                // 캐릭터를 비활성화 상태로 설정 (위치 이동, Renderer/Collider 비활성화)
                SetCharacterDespawned(playerCharacter);

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
            GameObject characterPrefab = Resources.Load<GameObject>("PlayerCharacters/" + "SPUM_" + prefabName);

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
                // 로딩 씬에서는 항상 일반 Instantiate 사용
                // (네트워크 동기화는 실제 소환 시에만 처리)
                // 이렇게 하면 씬 전환 시 PhotonView 동기화 문제를 방지할 수 있습니다.
                GameObject characterInstance;

                // 생성 위치를 카메라 밖으로 설정 (보이지 않게)
                Vector3 hiddenPosition = new Vector3(0, -1000, 0);

                // 일반 Instantiate 사용 (로딩 씬에서는 네트워크 동기화 없이 준비만)
                characterInstance = Instantiate(characterPrefab, hiddenPosition, Quaternion.identity);

                if (characterInstance == null)
                {
                    Debug.LogError($"[PlayerCharacterGroup] '{prefabName}' 프리팹 생성 실패!");
                    continue;
                }

                // 부모를 현재 클래스(PlayerCharacterGroup)로 설정
                characterInstance.transform.SetParent(transform);

                characterInstance.name = $"{prefabName}_{j}"; // 이름 설정 (인덱스 포함)

                // PlayerCharacter 컴포넌트 가져오기
                PlayerCharacter playerCharacter = characterInstance.GetComponent<PlayerCharacter>();

                if (playerCharacter == null)
                {
                    Debug.LogWarning($"[PlayerCharacterGroup] '{prefabName}' 프리팹에 PlayerCharacter 컴포넌트가 없습니다!");
                    Destroy(characterInstance);
                    continue;
                }

                // PhotonView가 있으면 ViewID를 0으로 초기화 (아직 네트워크에 등록하지 않음)
                PhotonView pv = characterInstance.GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.ViewID = 0; // 나중에 실제 소환 시 네트워크에 등록
                }

                // 캐릭터 이름 설정
                playerCharacter.Name = characterName;

                // 씬 전환 시에도 파괴되지 않도록 설정
                DontDestroyOnLoad(characterInstance);

                // List에 추가
                characterList.Add(playerCharacter);

                // 캐릭터를 비활성화 상태로 설정 (위치 이동, Renderer/Collider 비활성화)
                SetCharacterDespawned(playerCharacter);
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

        // 네트워크 환경에서는 PhotonNetwork.Instantiate를 사용하여 모든 클라이언트에 오브젝트 생성
        // 풀링 시스템은 로컬 참조용으로만 사용
        PlayerCharacter character = null;
        PhotonView characterPV = null;
        
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // PhotonNetwork.Instantiate를 사용하여 네트워크 오브젝트 생성
            string prefabPath = "PlayerCharacters/SPUM_" + characterName.ToString();
            Vector3 hiddenPosition = new Vector3(0, -1000, 0);
            
            GameObject networkInstance = PhotonNetwork.Instantiate(
                prefabPath,
                hiddenPosition,
                Quaternion.identity,
                0,
                null
            );
            
            if (networkInstance == null)
            {
                Debug.LogError($"[PlayerCharacterGroup] PhotonNetwork.Instantiate 실패: {prefabPath}");
                return null;
            }
            
            character = networkInstance.GetComponent<PlayerCharacter>();
            if (character == null)
            {
                Debug.LogError($"[PlayerCharacterGroup] 네트워크 오브젝트에 PlayerCharacter 컴포넌트가 없습니다!");
                PhotonNetwork.Destroy(networkInstance);
                return null;
            }
            
            // 캐릭터 이름 설정
            character.Name = characterName;
            
            // 부모를 PlayerCharacterGroup으로 설정
            networkInstance.transform.SetParent(transform);
            
            // PhotonView 소유권 확인
            characterPV = character.GetComponent<PhotonView>();
            if (characterPV != null && !characterPV.IsMine)
            {
                characterPV.TransferOwnership(PhotonNetwork.LocalPlayer);
            }
            
            Debug.Log($"[PlayerCharacterGroup] 네트워크 캐릭터 생성: {characterName} (ViewID: {characterPV?.ViewID})");
        }
        else
        {
            // 오프라인 모드: 풀에서 가져오기
            foreach (PlayerCharacter charInList in characterList)
            {
                if (charInList != null && IsCharacterDespawned(charInList))
                {
                    character = charInList;
                    break;
                }
            }
            
            if (character == null)
            {
                Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터 풀이 모두 사용 중입니다! (풀 크기: {characterList.Count})");
                return null;
            }
            
            // 오프라인 모드에서도 PhotonView 가져오기
            characterPV = character.GetComponent<PhotonView>();
        }

        // 캐릭터 활성화 (로컬에서 먼저 실행)
        SetCharacterSpawned(character);

        // 네트워크 동기화: 모든 클라이언트에 Renderer/Collider 활성화 전달
        if (characterPV != null && characterPV.ViewID != 0 && characterPV.IsMine)
        {
            // 코루틴을 사용하여 PhotonView 등록이 완전히 완료된 후 RPC 호출
            StartCoroutine(DelayedSpawnSync(characterPV));
        }

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

        // PhotonNetwork.Instantiate로 생성된 네트워크 오브젝트인지 확인
        PhotonView characterPV = character.GetComponent<PhotonView>();
        bool isNetworkObject = characterPV != null && characterPV.ViewID != 0 && 
                               PhotonNetwork.IsConnected && PhotonNetwork.InRoom;

        // 네트워크 오브젝트인 경우 풀에 속하지 않을 수 있으므로 체크 생략
        if (!isNetworkObject)
        {
            // 풀 오브젝트인 경우에만 List에 속하는지 확인
            if (!characterList.Contains(character))
            {
                Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 이 그룹에 속하지 않습니다!");
                return;
            }
        }

        // 이미 비활성화되어 있으면 그대로 반환
        if (IsCharacterDespawned(character))
        {
            Debug.LogWarning($"[PlayerCharacterGroup] '{characterName}' 캐릭터가 이미 비활성화되어 있습니다!");
            return;
        }

        // 네트워크 오브젝트인 경우 PhotonNetwork.Destroy로 제거
        if (isNetworkObject && characterPV != null && characterPV.IsMine)
        {
            PhotonNetwork.Destroy(character.gameObject);
            Debug.Log($"[PlayerCharacterGroup] 네트워크 캐릭터 제거: {characterName}");
        }
        else
        {
            // 풀 오브젝트인 경우 비활성화만 수행
            SetCharacterDespawned(character);
        }

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

        // 활성화된 캐릭터 찾기 (Renderer가 활성화된 캐릭터)
        PlayerCharacter character = null;
        foreach (PlayerCharacter charInList in characterList)
        {
            if (charInList != null && !IsCharacterDespawned(charInList))
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

    /// <summary>
    /// 캐릭터가 비활성화 상태인지 확인 (Renderer가 비활성화되어 있는지)
    /// </summary>
    private bool IsCharacterDespawned(PlayerCharacter character)
    {
        if (character == null)
            return true;

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            // Renderer가 하나라도 비활성화되어 있으면 비활성화 상태로 간주
            return !renderers[0].enabled;
        }

        // Renderer가 없으면 activeSelf로 판단
        return !character.gameObject.activeSelf;
    }

    /// <summary>
    /// 캐릭터를 활성화 상태로 설정 (Renderer/Collider 활성화)
    /// </summary>
    private void SetCharacterSpawned(PlayerCharacter character)
    {
        if (character == null)
            return;

        // GameObject 활성화 (항상 활성화 상태 유지)
        character.gameObject.SetActive(true);

        
        if (character.Model != null)
        {
            character.Model.OverrideControllerInit();
        }


        // Renderer 활성화
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        // Collider 활성화
        Collider[] colliders = character.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        // PlayerCharacter 컴포넌트 활성화
        character.enabled = true;

        // MirroringObject 초기화 확인
        if (character.MirroringObject != null)
        {
            // 원본 위치가 초기화되지 않았다면 현재 위치로 설정
            if (!character.MirroringObject.IsOriginalPositionInitialized())
            {
                character.MirroringObject.SetOriginalPosition(character.transform.position);
            }

            // 상대방 캐릭터인 경우 즉시 미러링 적용
            if (character.MirroringObject.ShouldApplyMirroring())
            {
                character.MirroringObject.ApplyMirroringPosition();
            }
        }
    }

    /// <summary>
    /// 캐릭터를 비활성화 상태로 설정 (위치 이동, Renderer/Collider 비활성화)
    /// </summary>
    private void SetCharacterDespawned(PlayerCharacter character)
    {
        if (character == null)
            return;

        // 위치를 카메라 밖으로 이동
        character.transform.position = new Vector3(0, -1000, 0);

        // Renderer 비활성화 (렌더링 부하 감소)
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        // Collider 비활성화
        Collider[] colliders = character.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        // PlayerCharacter 컴포넌트 비활성화 (로직 비활성화)
        character.enabled = false;

        // GameObject는 활성화 상태로 유지 (PhotonView 동기화를 위해)
    }

    /// <summary>
    /// PhotonView 등록이 완료된 후 RPC 호출을 지연시키는 코루틴
    /// </summary>
    private IEnumerator DelayedSpawnSync(PhotonView pv)
    {
        // 한 프레임 대기하여 PhotonView 등록이 완전히 완료되도록 함
        yield return null;
        
        // 다시 한 번 확인하여 안전하게 RPC 호출
        if (pv != null && pv.ViewID != 0 && pv.IsMine && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            pv.RPC("SetCharacterSpawnedSync", RpcTarget.All);
            Debug.Log($"[PlayerCharacterGroup] 캐릭터 소환 동기화 RPC 전송: ViewID={pv.ViewID}");
        }
        else
        {
            Debug.LogWarning($"[PlayerCharacterGroup] PhotonView가 준비되지 않아 동기화 RPC를 전송할 수 없습니다. ViewID={pv?.ViewID}, IsMine={pv?.IsMine}");
        }
    }
}

