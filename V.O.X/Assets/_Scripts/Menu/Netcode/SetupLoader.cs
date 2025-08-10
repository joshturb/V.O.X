using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SetupLoader : MonoBehaviour
{
	public string mainSceneName = "_Menu";
	void Start()
	{
		StartCoroutine(LoadMainScene());
	}
	private IEnumerator LoadMainScene()
	{
		// Wait until NetworkManager.Singleton is not null
		while (NetworkManager.Singleton == null)
		{
			yield return null;
		}
		SceneManager.LoadScene(mainSceneName);
	}
}
