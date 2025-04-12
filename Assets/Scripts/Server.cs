using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Server : MonoBehaviour
{
    string _ticketId;


    public float checkInterval = 1f; // Time interval to check for player connections
    private MatchmakingResults payloadAllocation;
    private bool allPlayersConnected = false;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(StartServer());
        //StartCoroutine(ApproveBackfillTicketEverySecond());
    }

    async Awaitable StartServer()
    {
        await UnityServices.InitializeAsync();
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return;
        }
        var server = MultiplayService.Instance.ServerConfig;


        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport component not found on NetworkManager!");
            return;
        }
        transport.SetConnectionData("0.0.0.0", server.Port);
        Debug.Log("Network Transport " + transport.ConnectionData.Address + ":" + transport.ConnectionData.Port);


        if (!NetworkManager.Singleton.StartServer())
        {
            Debug.LogError("Failed to start server");
            throw new Exception("Failed to start server");
        }

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => { Debug.Log("Client connected"); };
        NetworkManager.Singleton.OnServerStopped += (reason) => { Debug.Log("Server stopped"); };
        NetworkManager.Singleton.SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);
        Debug.Log($"Started Server {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");

        await CheckPlayers();

        var callbacks = new MultiplayEventCallbacks();
        callbacks.Allocate += OnAllocate;
        callbacks.Deallocate += OnDeallocate;
        callbacks.Error += OnError;
        callbacks.SubscriptionStateChanged += OnSubscriptionStateChanged;

        while (MultiplayService.Instance == null)
        {
            await Awaitable.NextFrameAsync();
        }

        // We must then subscribe.
        var events = await MultiplayService.Instance.SubscribeToServerEventsAsync(callbacks);
        //await CreateBackfillTicket();
    }



    async Task CheckPlayers()
    {
        // Fetch the payload allocation and get the players
        payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        StartCoroutine(CheckPlayerConnections());
    }

    private IEnumerator CheckPlayerConnections()
    {
        while (!allPlayersConnected)
        {
            // Get the connected player list
            var connectedPlayerList = NetworkManager.Singleton.ConnectedClientsList;

            // Get all player IDs from the payload
            var expectedPlayerIds = new List<ulong>();

            foreach (var player in payloadAllocation.MatchProperties.Players)
            {
                if (ulong.TryParse(player.Id, out ulong id))
                {
                    expectedPlayerIds.Add(id);
                }
                else
                {
                    Debug.LogWarning($"Invalid player ID received in payload: {player.Id}");
                }
            }

            // Get all connected player IDs
            var connectedPlayerIds = connectedPlayerList.Select(c => c.ClientId).ToList();

            // Check if all expected players are connected
            if (expectedPlayerIds.All(id => connectedPlayerIds.Contains(id)))
            {
                allPlayersConnected = true;
                OnAllPlayersConnected();
                yield break; // Exit the coroutine once all players are connected
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void OnAllPlayersConnected()
    {
        Debug.Log("All players are connected. Triggering the next step...");
        // Call the desired function once all players are connected
        StartGame();
    }

    private void StartGame()
    {
        Debug.Log("Starting the game...");
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        // Add logic to start the game here
    }

    void OnSubscriptionStateChanged(MultiplayServerSubscriptionState obj)
    {
        Debug.Log($"Subscription state changed: {obj}");
    }

    void OnError(MultiplayError obj)
    {
        Debug.Log($"Error received: {obj}");
    }

    async void OnDeallocate(MultiplayDeallocation obj)
    {
        Debug.Log($"Deallocation received: {obj}");
        await MultiplayService.Instance.UnreadyServerAsync();
    }

    async void OnAllocate(MultiplayAllocation allocation)
    {
        Debug.Log($"Allocation received: {allocation}");
        await MultiplayService.Instance.ReadyServerForPlayersAsync();
    }

    async Task CreateBackfillTicket()
    {
        MatchmakingResults results =
            await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

        Debug.Log(
            $"Environment: {results.EnvironmentId} MatchId: {results.MatchId} MatchProperties: {results.MatchProperties}");

        var backfillTicketProperties = new BackfillTicketProperties(results.MatchProperties);

        string queueName = "test"; // must match the name of the queue you want to use in matchmaker
        string connectionString = MultiplayService.Instance.ServerConfig.IpAddress + ":" +
                                  MultiplayService.Instance.ServerConfig.Port;

        var options = new CreateBackfillTicketOptions(queueName,
            connectionString,
            new Dictionary<string, object>(),
            backfillTicketProperties);

        // Create backfill ticket
        Debug.Log("Requesting backfill ticket");
        _ticketId = await MatchmakerService.Instance.CreateBackfillTicketAsync(options);
    }

    IEnumerator ApproveBackfillTicketEverySecond()
    {
        for (int i = 4; i >= 0; i--)
        {
            Debug.Log($"Waiting {i} seconds to start backfill");
            yield return new WaitForSeconds(1f);
        }

        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (String.IsNullOrWhiteSpace(_ticketId))
            {
                Debug.Log("No backfill ticket to approve");
                continue;
            }

            Debug.Log("Doing backfill approval for _ticketId: " + _ticketId);
            yield return MatchmakerService.Instance.ApproveBackfillTicketAsync(_ticketId);
            Debug.Log("Approved backfill ticket: " + _ticketId);
        }
    }
}