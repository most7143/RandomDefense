using DG.Tweening;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIGameFailed : MonoBehaviour
{
    public CanvasGroup Group;

    public TextMeshProUGUI CountText;

    public Button TitleButton;





    private void Start()
    {
        TitleButton.onClick.AddListener(GoToTitle);

        Group.alpha = 0;
        Group.blocksRaycasts = false;
        Group.interactable = false;
    }

    private void OnDestroy()
    {
        TitleButton.onClick.RemoveListener(GoToTitle);
    }

    public void FadeIn()
    {

        Group.DOFade(1, 1f).OnComplete(() =>
        {
            Group.blocksRaycasts = true;
            Group.interactable = true;
        });
    }

    public void GoToTitle()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("TitleScene");

    }








}
