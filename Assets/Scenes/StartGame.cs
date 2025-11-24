using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FB;

public class StartGame : MonoBehaviour
{


    void Start()
    {
    }


    void Update()
    {
        if (Auth.AuthCompleted && Input.touchCount >= 1) {
            if( Input.touches[0].phase == TouchPhase.Began ){
                StartCoroutine(LoadScene());
            }
        }
    }

    IEnumerator LoadScene() {
        AsyncOperation loadScene = SceneManager.LoadSceneAsync("GameScene");
        while(!loadScene.isDone) {
            yield return null;
        }
    }
}
