using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // Define the different states of the game
    public enum GameState
    {
        Gameplay,
        Paused,
        GameOver,
        LevelUp
    }

    // Store the current state of the game
    public GameState currentState;

    // Store the previous state of the game before it was paused
    public GameState previousState;

    [Header("Damage Text Settings")]
    public Canvas damageTextCanvas;
    public float textFontSize = 20;
    public TMP_FontAsset textFont;
    public Camera referenceCamera;

    [Header("Screens")]
    public GameObject pauseScreen;
    public GameObject resultsScreen;
    public GameObject levelUpScreen;
    int stackedLevelUps = 0; // If we try to StartLevelUp() multiple times.

    [Header("Results Screen Displays")]
    public Image chosenCharacterImage;
    public TMP_Text chosenCharacterName;
    public TMP_Text levelReachedDisplay;
    public TMP_Text timeSurvivedDisplay;

    [Header("Stopwatch")]
    public float timeLimit; // The time limit in seconds
    float stopwatchTime; // The current time elapsed since the stopwatch started
    public TMP_Text stopwatchDisplay;

    // Reference to the player's game object
    public GameObject playerObject;

    // Getters for parity with older scripts.
    public bool isGameOver { get { return currentState == GameState.Paused; } }
    public bool choosingUpgrade { get { return currentState == GameState.LevelUp; } }

    void Awake()
    {
        //Warning check to see if there is another singleton of this kind already in the game
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("EXTRA " + this + " DELETED");
            Destroy(gameObject);
        }

        DisableScreens();
    }

    void Update()
    {
        switch (currentState)
        {
            case GameState.Gameplay:
                // Code for the gameplay state
                CheckForPauseAndResume();
                UpdateStopwatch();
                break;
            case GameState.Paused:
                // Code for the paused state
                CheckForPauseAndResume();
                break;
            case GameState.GameOver:
            case GameState.LevelUp:
                break;
            default:
                Debug.LogWarning("STATE DOES NOT EXIST");
                break;
        }
    }

    IEnumerator GenerateFloatingTextCoroutine(string text, Transform target, float duration = 1f, float speed = 50f)
    {
        // Start generating the floating text.
        GameObject textObj = new GameObject("Damage Floating Text");
        RectTransform rect = textObj.AddComponent<RectTransform>();
        TextMeshProUGUI tmPro = textObj.AddComponent<TextMeshProUGUI>();
        tmPro.text = text;
        tmPro.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmPro.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmPro.fontSize = textFontSize;
        if (textFont) tmPro.font = textFont;
        rect.position = referenceCamera.WorldToScreenPoint(target.position);

        // Makes sure this is destroyed after the duration finishes.
        Destroy(textObj, duration);

        // Parent the generated text object to the canvas.
        textObj.transform.SetParent(instance.damageTextCanvas.transform);
        textObj.transform.SetSiblingIndex(0);

        // Pan the text upwards and fade it away over time.
        WaitForEndOfFrame w = new WaitForEndOfFrame();
        float t = 0;
        float yOffset = 0;
        Vector3 lastKnownPosition = target.position;
        while (t < duration)
        {
            // If the RectTransform is missing for whatever reason, end this loop.
            if (!rect) break;

            // Fade the text to the right alpha value.
            tmPro.color = new Color(tmPro.color.r, tmPro.color.g, tmPro.color.b, 1 - t / duration);

            // Update the enemy's position if it is still around.
            if (target) lastKnownPosition = target.position;

            // Pan the text upwards.
            yOffset += speed * Time.deltaTime;
            rect.position = referenceCamera.WorldToScreenPoint(lastKnownPosition + new Vector3(0, yOffset));

            // Wait for a frame and update the time.
            yield return w;
            t += Time.deltaTime;
        }
    }

    public static void GenerateFloatingText(string text, Transform target, float duration = 1f, float speed = 1f)
    {
        // If the canvas is not set, end the function so we don't
        // generate any floating text.
        if (!instance.damageTextCanvas) return;

        // Find a relevant camera that we can use to convert the world
        // position to a screen position.
        if (!instance.referenceCamera) instance.referenceCamera = Camera.main;

        instance.StartCoroutine(instance.GenerateFloatingTextCoroutine(
            text, target, duration, speed
        ));
    }

    // Define the method to change the state of the game
    public void ChangeState(GameState newState)
    {
        previousState = currentState;
        currentState = newState;
    }

    public void PauseGame()
    {
        if (currentState != GameState.Paused)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f; // Stop the game
            pauseScreen.SetActive(true); // Enable the pause screen
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeState(previousState);
            Time.timeScale = 1f; // Resume the game
            pauseScreen.SetActive(false); //Disable the pause screen
        }
    }

    // Define the method to check for pause and resume input
    void CheckForPauseAndResume()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void DisableScreens()
    {
        pauseScreen.SetActive(false);
        resultsScreen.SetActive(false);
        levelUpScreen.SetActive(false);
    }

    public void GameOver()
    {
        timeSurvivedDisplay.text = stopwatchDisplay.text;

        // Set the Game Over variables here.
        ChangeState(GameState.GameOver);
        Time.timeScale = 0f; //Stop the game entirely
        DisplayResults();
    }

    void DisplayResults()
    {
        resultsScreen.SetActive(true);
    }

    public void AssignChosenCharacterUI(CharacterData chosenCharacterData)
    {
        chosenCharacterImage.sprite = chosenCharacterData.Icon;
        chosenCharacterName.text = chosenCharacterData.Name;
    }

    public void AssignLevelReachedUI(int levelReachedData)
    {
        levelReachedDisplay.text = levelReachedData.ToString();
    }

    void UpdateStopwatch()
    {
        stopwatchTime += Time.deltaTime;
        UpdateStopwatchDisplay();

        if (stopwatchTime >= timeLimit)
        {
            playerObject.SendMessage("Kill");
        }
    }

    void UpdateStopwatchDisplay()
    {
        // Calculate the number of minutes and seconds that have elapsed
        int minutes = Mathf.FloorToInt(stopwatchTime / 60);
        int seconds = Mathf.FloorToInt(stopwatchTime % 60);

        // Update the stopwatch text to display the elapsed time
        stopwatchDisplay.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StartLevelUp()
    {
        ChangeState(GameState.LevelUp);

        // If the level up screen is already active, record it.
        if (levelUpScreen.activeSelf) stackedLevelUps++;
        else
        {
            levelUpScreen.SetActive(true);
            Time.timeScale = 0f; //Pause the game for now
            playerObject.SendMessage("RemoveAndApplyUpgrades");
        }
    }

    public void EndLevelUp()
    {
        Time.timeScale = 1f;    //Resume the game
        levelUpScreen.SetActive(false);
        ChangeState(GameState.Gameplay);

        if (stackedLevelUps > 0)
        {
            stackedLevelUps--;
            StartLevelUp();
        }
    }
}