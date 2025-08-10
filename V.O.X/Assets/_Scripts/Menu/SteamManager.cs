using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using Steamworks.Data;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using Steamworks;
using TMPro;

public class SteamManager : Singleton<SteamManager>
{
	public static Lobby? currentLobby;
	[SerializeField] private TMP_InputField maxPlayersInputField;
	[SerializeField] private TMP_InputField lobbyNameInputField;
	[SerializeField] private Toggle IsLobbyPublic;
	[SerializeField] private string sceneName = "_Game";
	public const string lobbyName = "V.O.X Lobby";

	void OnEnable()
	{
		SteamMatchmaking.OnLobbyCreated += LobbyCreated;
		SteamMatchmaking.OnLobbyEntered += LobbyEntered;
		SteamFriends.OnGameLobbyJoinRequested += FriendsListJoinRequested;
	}

	void OnDisable()
	{
		SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
		SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
		SteamFriends.OnGameLobbyJoinRequested -= FriendsListJoinRequested;
	}

	public async void HostLobby()
	{
		int memberLimit = int.Parse(maxPlayersInputField.text);
		if (memberLimit > 6)
		{
			memberLimit = 6;
		}
		await SteamMatchmaking.CreateLobbyAsync(memberLimit);
	}

	private void LobbyCreated(Result result, Lobby lobby)
	{
		if (result == Result.OK)
		{
			if (IsLobbyPublic.isOn)
			{
				lobby.SetPublic();
			}
			lobby.SetData(lobbyName, lobbyNameInputField.text);
			lobby.SetJoinable(true);
			NetworkManager.Singleton.ConnectionApprovalCallback = (connectionApprovalRequest, connectionApprovalResponse) =>
			{
				connectionApprovalResponse.Approved = true;
				connectionApprovalResponse.CreatePlayerObject = false;
			};
			NetworkManager.Singleton.StartHost();
			NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

			NetworkManager.Singleton.ConnectionApprovalCallback = null;
		}
	}

	public async void JoinLobby(SteamId lobbyId)
	{
		await SteamMatchmaking.JoinLobbyAsync(lobbyId);
	}

	private async void FriendsListJoinRequested(Lobby lobby, SteamId id)
	{
		await lobby.Join();
	}

	private void LobbyEntered(Lobby lobby)
	{
		currentLobby = lobby;
		Debug.Log($"Joined [ {lobby.Owner.Name} ]'s Lobby With ID: {lobby.Id}");

		if (NetworkManager.Singleton.IsHost)
		{
			return;
		}

		NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
		NetworkManager.Singleton.StartClient();
	}

	public void LeaveLobby()
	{
		currentLobby?.Leave();
		currentLobby = null;
		NetworkManager.Singleton.Shutdown();
	}

	// todo remove when done 
	public async void FastCreateForDev()
	{
		lobbyNameInputField.text = "Dev Lobby";
		IsLobbyPublic.isOn = true;
		await SteamMatchmaking.CreateLobbyAsync(6);
	}
}
