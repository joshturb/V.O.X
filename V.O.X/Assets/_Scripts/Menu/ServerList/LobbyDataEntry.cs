using UnityEngine;
using Steamworks;
using TMPro;

public struct LobbyData{
    public SteamId lobbyId;
    public int currentPlayers;
    public int maxPlayers;
    public string lobbyName;
}
public class LobbyDataEntry : MonoBehaviour
{
    public LobbyData lobbyData;
    public TMP_Text lobbyNameText;
    public TMP_Text lobbyUserText;

    public void SetLobbyData()
    {
        lobbyNameText.text = lobbyData.lobbyName == "" ? "Unknown" : lobbyData.lobbyName;
        lobbyUserText.text = lobbyData.currentPlayers + " / " + lobbyData.maxPlayers;
    }
    public void JoinLobby()
	{
		if (lobbyData.maxPlayers > lobbyData.currentPlayers)
        {
            SteamManager.Instance.JoinLobby(lobbyData.lobbyId);
        }
	}
}
