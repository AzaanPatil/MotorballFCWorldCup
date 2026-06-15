using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void StartMatch()
    {
        SceneManager.LoadScene("GameScene");
    }
}