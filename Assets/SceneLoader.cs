using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called before the first frame update

    public void LoadScene1()
    {
        // Only specifying the sceneName or sceneBuildIndex will load the Scene with the Single mode
        SceneManager.LoadScene("Scenes/Scene1");
    }
    public void LoadScene2()
    {
        // Only specifying the sceneName or sceneBuildIndex will load the Scene with the Single mode
        SceneManager.LoadScene("Scenes/Scene2");
    }

    public void LoadScene3()
    {
        // Only specifying the sceneName or sceneBuildIndex will load the Scene with the Single mode
        
        SceneManager.LoadScene("Scenes/TwistOfFates");
    }

    public void LoadScene4()
    {
        // Only specifying the sceneName or sceneBuildIndex will load the Scene with the Single mode

        SceneManager.LoadScene("Scenes/settings scene");
    }
}
