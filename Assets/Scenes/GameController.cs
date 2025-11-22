using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class GameController : MonoBehaviour
{
    private const string ScorePrefKey = "ShareIQScore";

    GameBoard gameBoard;
    GameObject ScoreIQ;
    GameObject PlayAgain;
    GameObject Share;
    GameObject ShareIQCanvas;
    Canvas ShareIQCanvasSR;
    Text ScoreIQText;
    float TimeLeft = 60.0f;
    bool bonusTileSpawned = false;
    bool isGameOver = false;
    bool isLoadingShareScene = false;

    GameObject timer;
    Text timerText;

    GameObject score;

    Text scoreText;
    Button shareButton;
    Button playAgainButton;


    void Start()
    {
        ScoreIQ = GameObject.Find("IQScore");
        PlayAgain = GameObject.Find("PlayAgain");
        Share = GameObject.Find("Share");
        ShareIQCanvas = GameObject.Find("ShareIQCanvas");
        ScoreIQText = ScoreIQ.GetComponent<Text>();
        playAgainButton = PlayAgain.GetComponent<Button>();
        shareButton = Share.GetComponent<Button>();
        if (shareButton != null)
        {
            shareButton.enabled = false;
            shareButton.interactable = false;
            shareButton.gameObject.SetActive(false);
        }
        ShareIQCanvasSR = ShareIQCanvas.GetComponent<Canvas>();
        if (ShareIQCanvasSR != null)
        {
            ShareIQCanvasSR.enabled = false;
        }
        playAgainButton.onClick.AddListener(delegate { playAgain(); });
        if (shareButton != null)
        {
            shareButton.onClick.AddListener(LoadShareScene);
        }
        gameBoard = new GameBoard(this);
        gameBoard.buildGameBoard();
        bonusTileSpawned = false;
        timer = GameObject.Find("Timer");
        score = GameObject.Find("Score");
        timerText = timer.GetComponent<Text>();
        scoreText = score.GetComponent<Text>();
    }

    
    void Update()
    {   
        if (isGameOver)
        {
            return;
        }

        TimeLeft -= Time.deltaTime;
        if (TimeLeft < 0.0f)
        {
            TimeLeft = 0.0f;
        }
        timerText.text = Math.Floor(TimeLeft).ToString();
        if (TimeLeft <= 0.0f) {
            EndGame();
            return;
        }
        scoreText.text = gameBoard.getScore().ToString();
        ScoreIQText.text = gameBoard.getScore().ToString();

        /* Temporarily disabled: late-round Level 2 tile spawn.
        if (gameBoard != null) {
            if (!bonusTileSpawned && TimeLeft <= 5.0f && TimeLeft > 0.0f) {
                if (gameBoard.SpawnBonusTile()) {
                    bonusTileSpawned = true;
                }
            } else if (bonusTileSpawned && !gameBoard.HasActiveBonusTile()) {
                bonusTileSpawned = false;
            }
        }
        */

        if (Input.touchCount >= 1) {
            if( Input.touches[0].phase == TouchPhase.Began ){
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.touches[0].position), Vector2.zero);
                if(hit.collider) {
                    gameBoard.addPoints(hit.collider.gameObject.name, hit, setTime);
                }  
            }
        }
    }

    public void setTime(float timeAdded){
        TimeLeft = TimeLeft + timeAdded;
    }

    public float getTime(){
        return TimeLeft;
    }

    void playAgain() {
        ShareIQCanvasSR.enabled = false;
        TimeLeft = 60f;
        scoreText.text = "0";
        ScoreIQText.text = "0";
        gameBoard = new GameBoard(this);
        gameBoard.buildGameBoard();
        bonusTileSpawned = false;
        isGameOver = false;
        isLoadingShareScene = false;
        if (shareButton != null)
        {
            shareButton.enabled = false;
            shareButton.interactable = false;
            shareButton.gameObject.SetActive(false);
        }
    }

    void LoadShareScene() {
        if (isLoadingShareScene) {
            return;
        }

        SaveScore();
        isLoadingShareScene = true;
        StartCoroutine(LoadShareSceneAsync());
    }

    IEnumerator LoadShareSceneAsync() {
        AsyncOperation loadScene = SceneManager.LoadSceneAsync("ShareIQScene");
        while(!loadScene.isDone) {
            yield return null;
        }
    }

    void EndGame() {
        if (isGameOver) {
            return;
        }

        TimeLeft = 0.0f;
        timerText.text = "0";
        isGameOver = true;
        gameBoard.removeGamePieces();
        LoadShareScene();
    }

    void SaveScore() {
        int scoreValue = gameBoard != null ? gameBoard.getScore() : 0;
        PlayerPrefs.SetInt(ScorePrefKey, scoreValue);
        PlayerPrefs.Save();
    }
}
