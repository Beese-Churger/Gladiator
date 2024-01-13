using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject pauseUI;
    [SerializeField] GameManager GameManager;
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseUI.activeInHierarchy)
            {
                pauseUI.SetActive(false);
            }
            else
            {
                pauseUI.SetActive(true);
            }
        }
    }


}
