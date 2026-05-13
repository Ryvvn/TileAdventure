using UnityEngine;
using UnityEngine.SceneManagement;

namespace TileAdventure.Services
{
    public class SceneLoader
    {
        public async System.Threading.Tasks.Task LoadSceneAsync(string sceneName)
        {
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"Scene not found: {sceneName}");
                return;
            }

            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            asyncOp.allowSceneActivation = true;

            while (!asyncOp.isDone)
            {
                await System.Threading.Tasks.Task.Yield();
            }
        }
    }
}
