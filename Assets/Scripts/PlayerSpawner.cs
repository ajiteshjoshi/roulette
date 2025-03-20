using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{

    public GameObject playerPrefab; // Assign this in the inspector
    public float tableRadius = 5f;
    public Vector3 tableCenter = Vector3.zero;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnPlayers();
        }
    }

    
    private void SpawnPlayers()
    {
        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        float angleBetweenPlayers = 360f / playerCount;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            int playerIndex = (int)clientId;

            // Calculate the angle for this player
            float angle = playerIndex * angleBetweenPlayers;

            // Convert angle to radians for Mathf.Cos and Mathf.Sin
            float radians = angle * Mathf.Deg2Rad;

            // Calculate the position around the table
            Vector3 spawnPosition = tableCenter + new Vector3(
                Mathf.Cos(radians) * tableRadius,
                0,
                Mathf.Sin(radians) * tableRadius
            );

            // Spawn the player prefab
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            NetworkObject networkObject = player.GetComponent<NetworkObject>();

            // Spawn the player on the network and assign ownership to the client
            networkObject.SpawnAsPlayerObject(clientId);

            // Rotate the player to face the center of the table
            player.transform.LookAt(tableCenter);
            player.transform.rotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y, 0);
        }
    }
}

