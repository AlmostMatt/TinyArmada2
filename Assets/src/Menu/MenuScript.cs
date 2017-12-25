using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour {
	private static string GAME_SCENE = "GameScene";
	private static string MENU_SCENE = "MainMenu";

	public void PlayGame() {
		SceneManager.LoadScene(GAME_SCENE, LoadSceneMode.Single);
	}
}
