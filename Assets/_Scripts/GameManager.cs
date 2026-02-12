using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    Entity playerEntity;
    WaveSpawner waveSpawner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
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
    }

    void Victory()
    {
        waveSpawner.OnWavesCompleted -= Victory;
    }
}