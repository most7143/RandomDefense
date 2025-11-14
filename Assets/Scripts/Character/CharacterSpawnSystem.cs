using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;


public class CharacterSpawnSystem : MonoBehaviourPunCallbacks
{
    public static CharacterSpawnSystem Instance { get; private set; }

    public int SpawnLevel=0;
    private int maxSpawnLevel=11;

    public Dictionary<CharacterGrades, int> CharacterSpawnChances = new Dictionary<CharacterGrades, int>();

    public Dictionary<CharacterGrades, List<CharacterNames>> GradeCharacterNames = new Dictionary<CharacterGrades, List<CharacterNames>>();


    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            InitializeCharacterNames(); // 초기화 추가
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        LevelUpSpawnLevel();
    }

    private void InitializeCharacterNames()
    {
        GradeCharacterNames.Add(CharacterGrades.Normal, new List<CharacterNames>());
        GradeCharacterNames.Add(CharacterGrades.Rare, new List<CharacterNames>());
        GradeCharacterNames.Add(CharacterGrades.Epic, new List<CharacterNames>());



        GradeCharacterNames[CharacterGrades.Normal].Add(CharacterNames.ElfApprenticeArcher);
        GradeCharacterNames[CharacterGrades.Rare].Add(CharacterNames.ElfArcher);
        GradeCharacterNames[CharacterGrades.Epic].Add(CharacterNames.ElfSharpshooter);

        GradeCharacterNames[CharacterGrades.Normal].Add(CharacterNames.DemonApprenticeMage);
        GradeCharacterNames[CharacterGrades.Rare].Add(CharacterNames.DemonAdeptMage);
        GradeCharacterNames[CharacterGrades.Epic].Add(CharacterNames.DemonWizard);

        GradeCharacterNames[CharacterGrades.Normal].Add(CharacterNames.SkeletonMage);
        GradeCharacterNames[CharacterGrades.Rare].Add(CharacterNames.SkeletonAssassin);
        GradeCharacterNames[CharacterGrades.Epic].Add(CharacterNames.SkeletonBerserker);

        GradeCharacterNames[CharacterGrades.Normal].Add(CharacterNames.Adventurer);
        GradeCharacterNames[CharacterGrades.Rare].Add(CharacterNames.Knight);
        GradeCharacterNames[CharacterGrades.Epic].Add(CharacterNames.Swordmaster);

        GradeCharacterNames[CharacterGrades.Normal].Add(CharacterNames.Shieldman);
        GradeCharacterNames[CharacterGrades.Rare].Add(CharacterNames.Shieldwarden);
        GradeCharacterNames[CharacterGrades.Epic].Add(CharacterNames.Guardian);

    }


    public void LevelUpSpawnLevel()
    {  
        if(SpawnLevel >= maxSpawnLevel)
            return;

        SpawnLevel++;
        (float normal, float rare, float epic) = SummonProbability.GetProbabilities(SpawnLevel);
        CharacterSpawnChances[CharacterGrades.Normal] = (int)(normal * 100);
        CharacterSpawnChances[CharacterGrades.Rare] = (int)(rare * 100);
        CharacterSpawnChances[CharacterGrades.Epic] = (int)(epic * 100);

    }

    public CharacterGrades GetRandomCharacterGrade()
    {
        int randomValue = Random.Range(0, 100);
        if(randomValue < CharacterSpawnChances[CharacterGrades.Normal])
            return CharacterGrades.Normal;
        else if(randomValue < CharacterSpawnChances[CharacterGrades.Normal] + CharacterSpawnChances[CharacterGrades.Rare])
            return CharacterGrades.Rare;
        else
            return CharacterGrades.Epic;
    }

    public CharacterNames GetRandomCharacterName(CharacterGrades characterGrade)
    {
        if(GradeCharacterNames.ContainsKey(characterGrade) && 
           GradeCharacterNames[characterGrade] != null && 
           GradeCharacterNames[characterGrade].Count > 0)
        {
            return GradeCharacterNames[characterGrade][Random.Range(0, GradeCharacterNames[characterGrade].Count)];
        }
       
        return CharacterNames.None;
    }
}
