using Unity.Netcode;
using UnityEngine;

public class Revolver : NetworkBehaviour
{
    public static Revolver Instance;
    private bool[] chambers = new bool[6];
    private int currentChamber = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ResetRevolver();
        }
    }

    public void ResetRevolver()
    {
        // Clear all chambers
        for (int i = 0; i < chambers.Length; i++)
        {
            chambers[i] = false;
        }

        // Randomly place one bullet
        int bulletChamber = Random.Range(0, chambers.Length);
        chambers[bulletChamber] = true;
        Debug.Log($"Bullet placed in chamber {bulletChamber + 1}");
    }

    public void SpinChamber()
    {
        currentChamber = Random.Range(0, chambers.Length);
        Debug.Log($"Chamber spun to {currentChamber + 1}");
    }

    public bool PullTrigger()
    {
        bool result = chambers[currentChamber];
        currentChamber = (currentChamber + 1) % chambers.Length;
        Debug.Log(result ? "Bang! Bullet fired." : "Click. Safe.");
        return result;
    }
}