using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class EndGameUI : MonoBehaviour
{
    public static EndGameUI instance;

    [SerializeField] GameObject winLoseContainer;
    [SerializeField] GameObject winScreen;
    [SerializeField] GameObject loseScreen;
    [SerializeField] Button restartLevelButton;
    [SerializeField] Button mainMenùButton;

    private void Awake()
    {
        instance = this;
        winLoseContainer.SetActive(false);

        //winScreen.SetActive(false);
        //loseScreen.SetActive(false);

        if (restartLevelButton) restartLevelButton.onClick.AddListener(() =>
        {
            SceneChanger.Instance?.LoadSingleAsync(SceneManager.GetActiveScene().buildIndex);
        });

        if (mainMenùButton) mainMenùButton.onClick.AddListener(() =>
        {
            SceneChanger.Instance?.LoadSingleAsync(0); //00 mainMenu buildIndex
        });
    }

    private void OnDestroy()
    {
        if (restartLevelButton) restartLevelButton.onClick.RemoveListener(() =>
        {
            SceneChanger.Instance?.LoadSingleAsync(SceneManager.GetActiveScene().buildIndex);
        });

        if (mainMenùButton) mainMenùButton.onClick.RemoveListener(() =>
        {
            SceneChanger.Instance?.LoadSingleAsync(0); //00 mainMenu buildIndex
        });
    }

    public void ShowWinLose(bool win)
    {
        winLoseContainer.SetActive(true);
        winScreen.SetActive(win);
        loseScreen.SetActive(!win);
        
        if (loseScreen.activeSelf && (GameObject.FindWithTag("Player")) != null)
        {
            // 1. Nascondi il corpo del player (la capsula)
            // Cerchiamo il MeshRenderer sul player
            MeshRenderer playerMesh = (GameObject.FindWithTag("Player")).GetComponent<MeshRenderer>();
            if (playerMesh != null) playerMesh.enabled = false;

            // 2. Nascondi l'arma
            // Se l'arma è figlia del player, possiamo cercare i renderer nei figli
            MeshRenderer[] allRenderers = (GameObject.FindWithTag("Player")).GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in allRenderers)
            {
                renderer.enabled = false;
            }
        }

        Time.timeScale = 0f;
        Utilities.SetCursorLocked(false);
    }
}