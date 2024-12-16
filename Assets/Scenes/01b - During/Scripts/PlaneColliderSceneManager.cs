using UnityEngine;
using UnityEngine.SceneManagement;

public class ColliderSceneManager : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private string[] scenesToLoad;
    [SerializeField] private string[] scenesToUnload;

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // Drag your player object here

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Determine the direction the player is coming from
            Vector3 toPlayer = playerTransform.position - transform.position;
            float dotProduct = Vector3.Dot(transform.forward, toPlayer.normalized);

            if (dotProduct > 0) // Player entered from the "front" of the plane
            {
                Debug.Log("Player entered from the front.");
                LoadScenes();
            }
            else // Player entered from the "back" of the plane
            {
                Debug.Log("Player entered from the back.");
                UnloadScenes();
            }
        }
    }

    private void LoadScenes()
    {
        foreach (string scene in scenesToLoad)
        {
            if (!IsSceneLoaded(scene))
            {
                SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive).completed += (operation) =>
                {
                    Debug.Log($"Successfully loaded scene: {scene}");
                };
            }
        }
    }

    private void UnloadScenes()
    {
        foreach (string scene in scenesToUnload)
        {
            if (IsSceneLoaded(scene))
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
                if (unloadOperation != null)
                {
                    unloadOperation.completed += (operation) =>
                    {
                        Debug.Log($"Successfully unloaded scene: {scene}");
                    };
                }
                else
                {
                    Debug.LogWarning($"Scene {scene} is not unloadable or has dependencies.");
                }
            }
            else
            {
                Debug.LogWarning($"Scene {scene} is not currently loaded, skipping unload.");
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
