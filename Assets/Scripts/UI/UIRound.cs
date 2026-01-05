using TMPro;
using UnityEngine;

public class UIRound : MonoBehaviour
{
    [Tooltip("타이머를 표시할 UI Text (TextMeshProUGUI 또는 Text)")]
    [SerializeField]
    private TextMeshProUGUI timerText;

    [SerializeField]
    private TextMeshProUGUI roundText;

    [SerializeField]
    private TextMeshProUGUI monsterCountText;

    public void SetTime(float time)
    {
        int totalSeconds = Mathf.CeilToInt(time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void SetRound(int round)
    {
        if (roundText != null)
        {
            roundText.text = $"Round: {round}";
        }
    }

    public void SetMonsterCount(int count, int maxCount)
    {
        if (monsterCountText != null)
        {
            monsterCountText.text = $"{count}/{maxCount}";
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
