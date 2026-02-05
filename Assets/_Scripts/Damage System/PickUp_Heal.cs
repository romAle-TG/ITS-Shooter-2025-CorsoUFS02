using UnityEngine;

public class PickUp_Heal : PickUp
{
    [SerializeField] int healAmount = 1000;

    protected override void Absorption(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Health health))
        {
            health.Heal(healAmount);
            gameObject.SetActive(false);
        }
    }
}
