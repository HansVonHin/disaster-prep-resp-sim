using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStartupManager : MonoBehaviour
{
    [SerializeField] private string[] scenesToLoad; // Add the names of your full scenes here

    private void Start()
    {
        if (scenesToLoad == null || scenesToLoad.Length == 0)
        {
            Debug.LogWarning("No scenes specified to load in SceneStartupManager.");
            return;
        }

        foreach (string scene in scenesToLoad)
        {
            if (!string.IsNullOrEmpty(scene) && !IsSceneLoaded(scene))
            {
                SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive)
                    .completed += (operation) =>
                    {
                        Debug.Log($"Successfully loaded scene: {scene}");
                    };
            }
        }
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }
}
