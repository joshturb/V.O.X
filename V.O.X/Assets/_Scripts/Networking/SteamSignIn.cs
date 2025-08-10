using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class SteamSignIn : MonoBehaviour
{
	async void Start()
	{
		await UnityServices.InitializeAsync();
		SignIn();
	}

	private async void SignIn()
	{
		await AuthenticationService.Instance.SignInAnonymouslyAsync();
	}
}
