using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
public class PlayerManager : NetworkBehaviour
{
    

    private ulong playerId;

     public CinemachineCamera cm;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerId = NetworkManager.Singleton.LocalClientId;
            Debug.Log($"Player {playerId} Joined.");
            cm.Priority.Value = 1;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
       

        if (GameManager.Instance == null)
        {
            return;
        }

       

        
        if (GameManager.Instance.IsMyTurn(playerId))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SpinChamber();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                PullTrigger();
            }
        }
    }

    private void SpinChamber()
    {
        if (IsServer)
        {
            Revolver.Instance.SpinChamber();
        }
        else
        {
            SpinChamberServerRpc();
        }
    }

    [ServerRpc(RequireOwnership =false)]
    private void SpinChamberServerRpc()
    {
        Revolver.Instance.SpinChamber();
    }

    private void PullTrigger()
    {
       
            PullTriggerServerRpc();
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void PullTriggerServerRpc()
    {
        Debug.Log("Trigger Pulled");
        bool bulletFired = Revolver.Instance.PullTrigger();
        GameManager.Instance.EndTurn(bulletFired, playerId);
    }
}