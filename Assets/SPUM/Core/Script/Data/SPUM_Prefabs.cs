using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
public enum PlayerState
{
    IDLE,
    MOVE,
    ATTACK,
    DAMAGED,
    DEBUFF,
    DEATH,
    OTHER,
}
public class SPUM_Prefabs : MonoBehaviour
{
    public float _version;
    public bool EditChk;
    public string _code;
    public Animator _anim;
    public AnimatorOverrideController OverrideController;

    public string UnitType;
    public List<SpumPackage> spumPackages = new List<SpumPackage>();
    public List<PreviewMatchingElement> ImageElement = new();
    public List<SPUM_AnimationData> SpumAnimationData = new();
    public Dictionary<string, List<AnimationClip>> StateAnimationPairs = new();
    public List<AnimationClip> IDLE_List = new();
    public List<AnimationClip> MOVE_List = new();
    public List<AnimationClip> ATTACK_List = new();
    public List<AnimationClip> DAMAGED_List = new();
    public List<AnimationClip> DEBUFF_List = new();
    public List<AnimationClip> DEATH_List = new();
    public List<AnimationClip> OTHER_List = new();

 private RuntimeAnimatorController originalController; // 원본 컨트롤러 저장
    
    public void OverrideControllerInit()
    {
        Animator animator = _anim;
        
        // Animator가 없으면 리턴
        if (animator == null)
        {
            Debug.LogWarning("[SPUM_Prefabs] Animator가 없습니다.");
            return;
        }
        
        // 이미 OverrideController가 설정되어 있고 StateAnimationPairs가 초기화되어 있으면 리턴
        if (OverrideController != null && StateAnimationPairs != null && StateAnimationPairs.Count > 0)
        {
            // 이미 초기화되었으므로 리턴
            return;
        }
        
        // 원본 컨트롤러 가져오기
        RuntimeAnimatorController baseController = animator.runtimeAnimatorController;
        
        // 이미 AnimatorOverrideController인 경우 원본을 찾기
        if (baseController is AnimatorOverrideController overrideController)
        {
            // 원본 컨트롤러가 저장되어 있으면 사용
            if (originalController != null)
            {
                baseController = originalController;
            }
            else
            {
                // 원본 컨트롤러 가져오기
                baseController = overrideController.runtimeAnimatorController;
            }
        }
        
        // 원본 컨트롤러가 없으면 리턴
        if (baseController == null)
        {
            Debug.LogWarning("[SPUM_Prefabs] 원본 AnimatorController를 찾을 수 없습니다.");
            return;
        }
        
        // 원본 컨트롤러 저장
        if (originalController == null)
        {
            originalController = baseController;
        }
        
        // OverrideController 생성
        OverrideController = new AnimatorOverrideController();
        OverrideController.runtimeAnimatorController = baseController;

        // 모든 애니메이션 클립을 가져옵니다
        AnimationClip[] clips = baseController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            // 복제된 클립으로 오버라이드합니다
            OverrideController[clip.name] = clip;
        }

        animator.runtimeAnimatorController = OverrideController;
        
        // StateAnimationPairs 초기화
        if (StateAnimationPairs == null)
        {
            StateAnimationPairs = new Dictionary<string, List<AnimationClip>>();
        }
        
        foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
        {
            var stateText = state.ToString();
            if (!StateAnimationPairs.ContainsKey(stateText))
            {
                StateAnimationPairs[stateText] = new List<AnimationClip>();
            }
            
            switch (stateText)
            {
                case "IDLE":
                    StateAnimationPairs[stateText] = IDLE_List;
                    break;
                case "MOVE":
                    StateAnimationPairs[stateText] = MOVE_List;
                    break;
                case "ATTACK":
                    StateAnimationPairs[stateText] = ATTACK_List;
                    break;
                case "DAMAGED":
                    StateAnimationPairs[stateText] = DAMAGED_List;
                    break;
                case "DEBUFF":
                    StateAnimationPairs[stateText] = DEBUFF_List;
                    break;
                case "DEATH":
                    StateAnimationPairs[stateText] = DEATH_List;
                    break;
                case "OTHER":
                    StateAnimationPairs[stateText] = OTHER_List;
                    break;
            }
        }
    }

    public bool allListsHaveItemsExist(){
        List<List<AnimationClip>> allLists = new List<List<AnimationClip>>()
        {
            IDLE_List, MOVE_List, ATTACK_List, DAMAGED_List, DEBUFF_List, DEATH_List, OTHER_List
        };

        return allLists.All(list => list.Count > 0);
    }
    [ContextMenu("PopulateAnimationLists")]
    public void PopulateAnimationLists()
    {
        IDLE_List = new();
        MOVE_List = new();
        ATTACK_List = new();
        DAMAGED_List = new();
        DEBUFF_List = new();
        DEATH_List = new();
        OTHER_List = new();
        
        var groupedClips = spumPackages
        .SelectMany(package => package.SpumAnimationData)
        .Where(spumClip => spumClip.HasData && 
                        spumClip.UnitType.Equals(UnitType) && 
                        spumClip.index > -1 )
        .GroupBy(spumClip => spumClip.StateType)
        .ToDictionary(
            group => group.Key, 
            group => group.OrderBy(clip => clip.index).ToList()
        );
    // foreach (var item in groupedClips)
    // {
    //     foreach (var clip in item.Value)
    //     {
    //         Debug.Log(clip.ClipPath);
    //     }
    // }
        foreach (var kvp in groupedClips)
        {
            var stateType = kvp.Key;
            var orderedClips = kvp.Value;
            switch (stateType)
            {
                case "IDLE":
                    IDLE_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = IDLE_List;
                    break;
                case "MOVE":
                    MOVE_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = MOVE_List;
                    break;
                case "ATTACK":
                    ATTACK_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = ATTACK_List;
                    break;
                case "DAMAGED":
                    DAMAGED_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DAMAGED_List;
                    break;
                case "DEBUFF":
                    DEBUFF_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DEBUFF_List;
                    break;
                case "DEATH":
                    DEATH_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DEATH_List;
                    break;
                case "OTHER":
                    OTHER_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = OTHER_List;
                    break;
            }
        }
    
    }
  public void PlayAnimation(PlayerState PlayState, int index){
    Animator animator = _anim;
    
    // StateAnimationPairs가 초기화되지 않았으면 초기화
    if (StateAnimationPairs == null || StateAnimationPairs.Count == 0)
    {
        OverrideControllerInit();
    }
    
    var stateKey = PlayState.ToString();
    
    // 딕셔너리에 키가 있는지 확인
    if (!StateAnimationPairs.ContainsKey(stateKey))
    {
        Debug.LogWarning($"[SPUM_Prefabs] StateAnimationPairs에 '{stateKey}' 키가 없습니다. 초기화를 시도합니다.");
        OverrideControllerInit();
        
        // 다시 확인
        if (!StateAnimationPairs.ContainsKey(stateKey))
        {
            Debug.LogError($"[SPUM_Prefabs] '{stateKey}' 상태의 애니메이션을 찾을 수 없습니다.");
            return;
        }
    }
    
    var animations = StateAnimationPairs[stateKey];
    
    // 인덱스 범위 확인
    if (index < 0 || index >= animations.Count)
    {
        Debug.LogWarning($"[SPUM_Prefabs] '{stateKey}' 상태의 애니메이션 인덱스 {index}가 범위를 벗어났습니다. (Count: {animations.Count})");
        return;
    }
    
    //Debug.Log(OverrideController[PlayState.ToString()].name);
    OverrideController[stateKey] = animations[index];
    //Debug.Log( OverrideController[PlayState.ToString()].name);
    var StateStr = PlayState.ToString();

    bool isMove = StateStr.Contains("MOVE");
    bool isDebuff = StateStr.Contains("DEBUFF");
    bool isDeath = StateStr.Contains("DEATH");
    animator.SetBool("1_Move", isMove);
    animator.SetBool("5_Debuff", isDebuff);
    animator.SetBool("isDeath", isDeath);
    if(!isMove && !isDebuff)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        foreach (AnimatorControllerParameter parameter in parameters)
        {
            // if(parameter.type == AnimatorControllerParameterType.Bool){
            //     bool isBool = StateStr.ToUpper().Contains(parameter.name.ToUpper());
            //     animator.SetBool(parameter.name, isBool);
            // }
            if(parameter.type == AnimatorControllerParameterType.Trigger)
            {
                bool isTrigger = parameter.name.ToUpper().Contains(StateStr.ToUpper());
                if(isTrigger){
                     Debug.Log($"Parameter: {parameter.name}, Type: {parameter.type}");
                    animator.SetTrigger(parameter.name);
                }
            }
        }
    }
}

    AnimationClip LoadAnimationClip(string clipPath)
    {
        // "Animations" 폴더에서 애니메이션 클립 로드
        AnimationClip clip = Resources.Load<AnimationClip>(clipPath.Replace(".anim", ""));
        
        if (clip == null)
        {
            Debug.LogWarning($"Failed to load animation clip '{clipPath}'.");
        }
        
        return clip;
    }
}
