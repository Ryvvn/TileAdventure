using UnityEngine;
using UnityEngine.SceneManagement;

namespace TileAdventure.Services
{
    /// <summary>
    /// Async scene transition helper. Wraps SceneManager.LoadSceneAsync
    /// with a progress gate at 0.9 (activation held until fully loaded).
    ///
    /// Usage:  new SceneLoader().LoadSceneAsync("Gameplay");
    ///
    /// This is a plain C# class — no MonoBehaviour needed.
    /// </summary>
    public class SceneLoader
    {
        /// <summary>
        /// Load a scene by name asynchronously. The scene activates only after
        /// loading reaches 0.9 progress (all assets loaded, Awake ready to fire).
        /// </summary>
        public async System.Threading.Tasks.Task LoadSceneAsync(string sceneName)
        {
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"Scene not found in Build Settings: {sceneName}");
                return;
            }

            // Hold activation until fully loaded
            asyncOp.allowSceneActivation = false;

            // Wait for load to reach 90%
            while (asyncOp.progress < 0.9f)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            // Allow activation and wait for completion
            asyncOp.allowSceneActivation = true;

            while (!asyncOp.isDone)
            {
                await System.Threading.Tasks.Task.Yield();
            }
        }
    }
}
