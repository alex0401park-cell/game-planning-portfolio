using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void LoadSaveSelectScene()
    {
        SceneManager.LoadScene("SaveSelectScene");
    }

    public void LoadCharacterSelectScene()
    {
        SceneManager.LoadScene("CharacterSelectScene");
    }

    public void LoadCollectionScene()
    {
        SceneManager.LoadScene("CollectionScene");
    }

    public void LoadMapScene()
    {
        SceneManager.LoadScene("MapScene");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game Exit");
    }
}