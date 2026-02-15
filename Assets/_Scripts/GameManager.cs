using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Entity playerEntity;
    WaveSpawner waveSpawner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterSpawner(WaveSpawner _waveSpawner)
    {
        waveSpawner = _waveSpawner;
        waveSpawner.OnWavesCompleted += Victory;
    }

    public void RegisterPlayer(Entity _playerEntity)
    {
        playerEntity = _playerEntity;
        playerEntity.EntityHealth.OnDied += Defeat;
    }

    void Defeat(Health playerHealth)
    {
        playerEntity.EntityHealth.OnDied -= Defeat;
        // Chiamiamo l'UI di fine gioco passando a 'false' (sconfitta)
        EndGameUI.instance.ShowWinLose(false);
    }

    void Victory()
    {
        waveSpawner.OnWavesCompleted -= Victory;
        // Chiamiamo l'UI di fine gioco passando a 'true' (vittoria)
        EndGameUI.instance.ShowWinLose(true);
    }
}