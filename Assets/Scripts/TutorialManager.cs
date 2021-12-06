using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{

    [SerializeField]
    private GameObject[] clonePrefabs;

    [SerializeField]
    private PlayerMovement[] playerControllers;

    [SerializeField]
    private PlayerRecording[] playerRecordings;

    [SerializeField]
    private TextMeshProUGUI timer;

    [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField]
    private BallScript[] balls;

    [SerializeField]
    private GameObject helpPanel;
    [SerializeField]
    private GameObject preparePanel;
    private TextMeshProUGUI prepareText;

    [SerializeField]
    private GameObject pauseMenuPanel;

    [SerializeField]
    private GameObject endMenuPanel;

    [SerializeField]
    private TextMeshProUGUI winnerText;

    [SerializeField]
    private GameObject controlsPanel;

    [SerializeField]
    private GameObject[] playerPrefabs;

    [SerializeField]
    private GameObject mainPanel;

    private float timeRemaining = GameConfigurations.roundDuration;
    private int roundNumber = 1;

    private bool timerIsRunning = false;

    private bool gamePrepare = false;
    private bool gameStarted = false;
    private bool gamePaused = false;
    private bool gameEnded = false;

    private bool roundEnd = true;

    PlayerControls controls;

    private AudioManager audioManager;
    private AudioSource runningWithouBall;
    private AudioSource stadiumCrowd;
    private AudioSource gameTheme;

    private HashSet<int> playerTutorialFinished = new HashSet<int>();
    [SerializeField]
    private GameObject[] playerNextRoundReady;

    private void Awake()
    {

        var playerConfigs = PlayerConfigManager.Instance.GetPlayerConfigs().ToArray();
        var goals = GameObject.FindGameObjectsWithTag("Goals");

        for (int i = 0; i < playerConfigs.Length; i++)
        {
            var player = Instantiate(playerPrefabs[i], spawnPoints[i].position, spawnPoints[i].rotation, gameObject.transform);
            player.GetComponent<PlayerMovement>().InitializePlayer(playerConfigs[i]);
            playerRecordings[i] = player.GetComponent<PlayerRecording>();
            playerControllers[i] = player.GetComponent<PlayerMovement>();
            playerControllers[i].inTutorial = true;

            foreach (GameObject goal in goals) {
                goal.GetComponent<GoalPost>().playerMovements[i] = player.GetComponent<PlayerMovement>();
            }

        }

        controls = new PlayerControls();
        controls.MainMenu.StartGame.performed += ctx =>
        {
            if (!gameStarted && !gameEnded && !gamePrepare && roundEnd) {
                PrepareStartGame();
            }

            else if (gameStarted && !gameEnded && !gamePrepare && roundEnd) {
                PrepareNextRound();
            }

            else if (!gameStarted && gameEnded)
            {
                Debug.Log("game ended");
                CloneManager.DeleteClones();
                SceneManager.LoadScene("LobbyScene");
            }
        };

        controls.MainMenu.ShowControls.performed += ctx =>
        {
            if (!gameStarted && !gameEnded)
            {
                if (controlsPanel.activeInHierarchy)
                {
                    ShowControlsPanel(false);
                }
                else
                {
                    ShowControlsPanel(true);
                }
            }
        };

        controls.MainMenu.PauseGame.started += ctx =>
        {

            if (gameStarted && !gameEnded && !roundEnd)
            {
                if (!gamePaused)
                    PauseGame();
                else
                    ResumeGame();       
            }
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0f;

        CloneManager.Configure(clonePrefabs, playerRecordings);
        foreach (PlayerMovement player in playerControllers)
        {
            if (player.playerNumber == PlayerData.PlayerNumber.PlayerOne) {
                player.GetComponent<PlayerMovement>().spawnLocation = spawnPoints[0].position;
                player.GetComponent<PlayerMovement>().spawnRotation = spawnPoints[0].rotation.eulerAngles;
            }
            else {
                player.GetComponent<PlayerMovement>().spawnLocation = spawnPoints[1].position;
                player.GetComponent<PlayerMovement>().spawnRotation = spawnPoints[1].rotation.eulerAngles;
            }
            player.GetComponent<PlayerMovement>().enabled = false;
        }

        prepareText = preparePanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        audioManager = FindObjectOfType<AudioManager>();
        runningWithouBall = audioManager.GetAudio("Running");
        stadiumCrowd = audioManager.GetAudio("Crowd");
        gameTheme = audioManager.GetAudio("Game");
    }

    void Update()
    {
        if (gamePrepare) {
            if (timeRemaining > 0) {
                timeRemaining -= Time.unscaledDeltaTime;
                DisplaySecondsOnly(timeRemaining);
            }
            
            else {
                timeRemaining = 0;
                DisplaySecondsOnly(timeRemaining);
                gamePrepare = false;

                if (roundNumber == 1)
                    StartGame();
                else
                    StartRound();
            }
        }

        else {
            if (!gamePaused && gameStarted) {
                if (timerIsRunning) {
                    if (playerTutorialFinished.Count < 2) {

                    }

                    else {
                        timeRemaining = 0;
                        timerIsRunning = false;
                        CloneManager.AddClones();
                        FinishRound();
                    }
                }
            }

            foreach (var ball in balls) {
                if (!ball.enabled)
                    ball.enabled = true;
            }

            if (playerControllers[0].ShouldStopRunningSound() && playerControllers[1].ShouldStopRunningSound()) {
                if (runningWithouBall.isPlaying)
                    runningWithouBall.Stop();
            }
        }
    }

    public void PrepareNextRound() {
        foreach (PlayerMovement player in playerControllers) {
            player.GetComponent<PlayerMovement>().enabled = true;
        }

        doNextRoundStuff();
        foreach (PlayerMovement player in playerControllers) {
            player.GetComponent<PlayerMovement>().enabled = false;
        }
        foreach (UICloneNumberScript playerOverlayScript in FindObjectsOfType<UICloneNumberScript>()) {
            playerOverlayScript.HideCloneNumbers(true);
        }

        preparePanel.SetActive(false);
        helpPanel.SetActive(true);
        
        timer.gameObject.SetActive(true);
        timeRemaining = GameConfigurations.nearEndingTime;
        timer.transform.localScale *= 2;

        roundEnd = false;
        gamePrepare = true;

        playerTutorialFinished.Clear();
    }

    public void StartRound() {
        timer.gameObject.SetActive(false);
        Time.timeScale = GameConfigurations.normalTimeScale;

        timerIsRunning = true;
        timeRemaining = GameConfigurations.roundDuration;
        foreach (PlayerMovement player in playerControllers) {
            player.GetComponent<PlayerMovement>().enabled = true;
        }
        foreach (UICloneNumberScript playerOverlayScript in FindObjectsOfType<UICloneNumberScript>()) {
            playerOverlayScript.HideCloneNumbers(false);
        }

        gamePrepare = false;
    }

    public void FinishRound() {
        Time.timeScale = 0f;
        timerIsRunning = false;

        timeRemaining = GameConfigurations.roundDuration;
        foreach (PlayerMovement player in playerControllers) {
            player.GetComponent<PlayerMovement>().enabled = false;
        }

        preparePanel.SetActive(true);
        helpPanel.SetActive(false);
        playerNextRoundReady[0].SetActive(false);
        playerNextRoundReady[1].SetActive(false);

        prepareText.text = "Well Done!";

        roundEnd = true;
    }

    private void PrepareStartGame() {
        mainPanel.SetActive(true);
        preparePanel.SetActive(false);
        helpPanel.SetActive(true);

        timer.gameObject.SetActive(true);
        timeRemaining = GameConfigurations.nearEndingTime;
        timer.transform.localScale *= 2;

        gamePrepare = true;
    }

    private void StartGame() {
        gameStarted = true;
        gamePaused = false;
        roundEnd = false;

        Time.timeScale = GameConfigurations.normalTimeScale;
        timerIsRunning = true;
        timeRemaining = GameConfigurations.roundDuration;
        timer.gameObject.SetActive(false);
        foreach (PlayerMovement player in playerControllers) {
            player.GetComponent<PlayerMovement>().enabled = true;
        }

        foreach (var ball in balls) {
            ball.gameObject.SetActive(true);
        }

        stadiumCrowd.Play();
        gameTheme.Play();
    }

    public void ShowControlsPanel(bool value)
    {
        controlsPanel.SetActive(value);
    }

    private void DisplaySecondsOnly(float timeToDisplay) {
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timer.color = Color.red;
        timer.text = string.Format("{0}", seconds);
    }

    private void doNextRoundStuff()
    {
        timer.transform.localScale = Vector3.one;

        CloneManager.KillClones();
        //Debug.Log("started round " + roundNumber.ToString());

        if (roundNumber == GameConfigurations.numberOfRounds) {
            EndGame();
            return;
        }
        roundNumber++;
        timerIsRunning = true;
        
        timeRemaining = GameConfigurations.roundDuration + Math.Min((roundNumber - 1) * GameConfigurations.roundLengthIncrease, GameConfigurations.maxRoundLength);
        CloneManager.SpawnClones();
        foreach (PlayerMovement player in playerControllers) {
            player.Reset();
        }

        foreach (UICloneNumberScript playerOverlayScript in FindObjectsOfType<UICloneNumberScript>()) {
            playerOverlayScript.Reset();
        }

        foreach (var ball in balls) {
            ball.Reset();
        }
    }

    public void PauseGame()
    {
        gamePaused = true;
        Time.timeScale = 0f;
        pauseMenuPanel.SetActive(true);
        foreach (PlayerMovement player in playerControllers)
        {
            player.GetComponent<PlayerMovement>().enabled = false;
        }

        stadiumCrowd.Stop();
        gameTheme.Stop();
    }

    public void ResumeGame()
    {
        gamePaused = false;
        Time.timeScale = GameConfigurations.normalTimeScale;
        pauseMenuPanel.SetActive(false);
        foreach (PlayerMovement player in playerControllers)
        {
            player.GetComponent<PlayerMovement>().enabled = true;
        }

        stadiumCrowd.Play();
        gameTheme.Play();
    }

    public void EndGame()
    {
        stadiumCrowd.Stop();
        gameTheme.Stop();

        gameStarted = false;
        gameEnded = true;
        gamePaused = true;
        endMenuPanel.SetActive(true);
        winnerText.text = "Tutorial End!";

        Time.timeScale = 0f;
        foreach (PlayerMovement player in playerControllers)
        {
            player.GetComponent<PlayerMovement>().enabled = false;
        }
    }

    public float GetTimeLeft()
    {
        return timeRemaining;
    }

    public int GetRoundNumber()
    {
        return roundNumber;
    }

    private void OnEnable()
    {
        controls.MainMenu.Enable();
    }

    private void OnDisable()
    {
        controls.MainMenu.Disable();
    }

    public void ReadyPlayer(int index) {
        playerTutorialFinished.Add(index);
        playerNextRoundReady[index].SetActive(true);
        Debug.Log("Player ready in tutorial");
    }
}