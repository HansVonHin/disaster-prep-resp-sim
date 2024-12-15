using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStartupManager : MonoBehaviour
{
    [SerializeField] private string[] subScenesToLoad; // Add the sub-scene names in the Inspector

    private void Start()
    {
        if (subScenesToLoad == null || subScenesToLoad.Length == 0)
        {
            Debug.LogWarning("No sub-scenes specified to load in SceneStartupManager.");
            return;
        }

        foreach (string subScene in subScenesToLoad)
        {
            if (!string.IsNullOrEmpty(subScene) && !IsSceneLoaded(subScene))
            {
                SceneManager.LoadSceneAsync(subScene, LoadSceneMode.Additive)
                    .completed += (operation) =>
                    {
                        Debug.Log($"Successfully loaded sub-scene: {subScene}");
                        ActivateSceneGameObjects(subScene);
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

    private void ActivateSceneGameObjects(string sceneName)
    {
        Scene subScene = SceneManager.GetSceneByName(sceneName);
        if (subScene.isLoaded)
        {
            GameObject[] rootObjects = subScene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                rootObject.SetActive(true); // Ensure all root GameObjects are active
            }
        }
        else
        {
            Debug.LogWarning($"Scene {sceneName} is not loaded, so its GameObjects cannot be activated.");
        }
    }
}
