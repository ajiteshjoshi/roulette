using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    // Network variables to sync game state
    private NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);
    private NetworkVariable<int> roundNumber = new NetworkVariable<int>(1);

    // Track alive players
    private NetworkList<ulong> alivePlayers;

    // Reference to the revolver
    public Revolver revolver;

    public Image DeadImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Initialize the NetworkList
        alivePlayers = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize the game when the first player joins
            InitializeGame();
        }

        // Listen for changes to the current player turn
        currentPlayerTurn.OnValueChanged += OnPlayerTurnChanged;

        // Listen for changes to the alivePlayers list
        alivePlayers.OnListChanged += OnAlivePlayersChanged;
    }

    private void InitializeGame()
    {
        // Add all connected players to the alive list
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            alivePlayers.Add(client.Key);
            
        }

        // Start the first round
        StartRound();
    }

    private void StartRound()
    {
        Debug.Log($"Round {roundNumber.Value} Started!");

        // Reset the revolver for the new round
        revolver.ResetRevolver();

        // Start with the first player
        currentPlayerTurn.Value = 0;
    }

    public void EndTurn(bool bulletFired, ulong playerId)
    {
        if (isGameOver.Value) return;

        if (bulletFired)
        {
            // Eliminate the player
            EliminatePlayer(playerId);
            Debug.Log($"Player {playerId} is eliminated!");

            // Check if only one player is left
            if (alivePlayers.Count == 1)
            {
                EndGame(alivePlayers[0]);
                return;
            }

            // Start a new round if the bullet is fired
            StartNewRound();
        }
        else
        {
            // Move to the next player's turn
            int nextPlayerIndex = (currentPlayerTurn.Value + 1) % alivePlayers.Count;
            currentPlayerTurn.Value = nextPlayerIndex;
            Debug.Log($"Player {alivePlayers[nextPlayerIndex]}'s Turn.");
        }
    }

    private void StartNewRound()
    {
        roundNumber.Value++;
        StartRound();
    }

    private void EliminatePlayer(ulong playerId)
    {
        alivePlayers.Remove(playerId);

        // Notify all clients that the player is eliminated
        NotifyPlayerEliminatedClientRpc(playerId);
    }

    [ClientRpc]
    private void NotifyPlayerEliminatedClientRpc(ulong playerId)
    {

        Debug.Log($"Player {playerId} has been eliminated.");
        if(NetworkManager.LocalClientId == playerId)
        {
            DeadImage.gameObject.SetActive(true);
        }
       
    }

    private void EndGame(ulong winnerId)
    {
        isGameOver.Value = true;
        Debug.Log($"Game Over! Player {winnerId} wins!");

        // Notify all clients that the game is over
        NotifyGameOverClientRpc(winnerId);
    }

    [ClientRpc]
    private void NotifyGameOverClientRpc(ulong winnerId)
    {

        Debug.Log($"Game Over! Player {winnerId} wins!");
        DeadImage.gameObject.SetActive(true);
        if(winnerId == NetworkManager.LocalClientId)
        {
            DeadImage.color = Color.green;
        }
        
    }

    public bool IsMyTurn(ulong playerId)
    {
        if (alivePlayers.Count <=1) return false;
        bool isMyTurn = alivePlayers[currentPlayerTurn.Value] == playerId;
       
        return isMyTurn;
    }

    private void OnPlayerTurnChanged(int oldTurn, int newTurn)
    {
        if (alivePlayers.Count > 0)
        {
            
            Debug.Log($"Player {alivePlayers[newTurn]}'s Turn.");
        }
    }


    private void OnAlivePlayersChanged(NetworkListEvent<ulong> changeEvent)
    {
        Debug.Log($"Alive Players List Changed: {changeEvent.Value}");
    }
    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        currentPlayerTurn.OnValueChanged -= OnPlayerTurnChanged;
        alivePlayers.OnListChanged -= OnAlivePlayersChanged;
    }
}