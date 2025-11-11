using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using TMPro;

public class LobbyListManager : MonoBehaviour
{
    public GameObject lobbyDataItemPrefab;
    public GameObject lobbyListContent;
    public TMP_Dropdown dropdown;
    public List<GameObject> listOfLobbies = new();

    private void Start()
    {
        if (SteamClient.IsValid)
        {
            SearchForLobbies();
        }
        else
        {
            Debug.LogError("Steam is not initialized. Unable to search for lobbies.");
        }
    }

    public void DestroyLobbies()
    {
        foreach(var lobbyItem in listOfLobbies){
            Destroy(lobbyItem);
        }
        listOfLobbies.Clear();
    }   
    public async void SearchForLobbies()
    {
        if (listOfLobbies.Count > 0) { DestroyLobbies(); }

        Lobby[] lobbies = null; // Ensure lobbies is locally defined

        switch (dropdown.value)
        {
            case 0:
                lobbies = await SteamMatchmaking.LobbyList.FilterDistanceWorldwide().WithSlotsAvailable(1).RequestAsync();
                break;
            case 1:
                lobbies = await SteamMatchmaking.LobbyList.FilterDistanceFar().WithSlotsAvailable(1).RequestAsync();
                break;
            case 2:
                lobbies = await SteamMatchmaking.LobbyList.FilterDistanceClose().WithSlotsAvailable(1).RequestAsync();
                break;
        }

        // Check if lobbies is null
        if (lobbies != null)
        {
            DisplayLobbies(lobbies);
        }
        else
        {
            Debug.LogWarning("No lobbies found or an error occurred while searching for lobbies.");
        }
    }

    public void DisplayLobbies(Lobby[] lobbies)
    {
        for (int i = 0; i < lobbies.Length; i++)
        {
            if (string.IsNullOrEmpty(lobbies[i].GetData(SteamManager.lobbyName)))
                continue;

            GameObject createdItem = Instantiate(lobbyDataItemPrefab);
            
            // Check if the instantiated item has the LobbyDataEntry component
            if (!createdItem.TryGetComponent<LobbyDataEntry>(out var lobbyDataEntry))
            {
                Debug.LogError("LobbyDataEntry component is missing from the lobby data item prefab.");
                continue; // Skip this lobby if the component is missing
            }

            lobbyDataEntry.lobbyData.lobbyId = lobbies[i].Id;
            lobbyDataEntry.lobbyData.lobbyName = lobbies[i].GetData("GhostInTheGraveyard");
            lobbyDataEntry.lobbyData.currentPlayers = lobbies[i].MemberCount;
            lobbyDataEntry.lobbyData.maxPlayers = lobbies[i].MaxMembers;
            lobbyDataEntry.SetLobbyData();
            createdItem.transform.SetParent(lobbyListContent.transform, false);
			Vector3 localPos = createdItem.transform.localPosition;
			localPos.z = 0f;
			createdItem.transform.localPosition = localPos;
            createdItem.transform.localScale = Vector3.one;

            listOfLobbies.Add(createdItem);
        }
    }
}
