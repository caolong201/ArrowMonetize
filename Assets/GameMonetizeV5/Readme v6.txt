#UNTIY PLUGIN V6
############################
GameMonetize.com
############################

Setup:
 - Drag the prefab "GameMonetize" into your scene but dont rename this or else audio or game not pause
 - Replace the GAME_ID values with your own keys
 - Use GameMonetize.Instance.ShowAd() to show an advertisement or drag in second scene the prefab "AdsOnStart"
 - Make use of the events GameMonetize.OnResumeGame and GameMonetize.OnPauseGame for resuming/pausing your game in between ads
 - Do not rename prefab GameMonetize or ResumeGame() & PauseGame() will not work anymore
 - if you use unity 5.6, 2017, 2018, 2019, 2020, 2022, 2023 you will need to create new prefab and drag script "GameMonetize" 
 - you can use WebGLTemplates depend of your unity version, go to File/Build Settings/Player Settings/Resolution and Presentation and can choose any WebGL Template depend of your unity version
 - First ADS must show on button PLAY or game loading, then you can add ads show on WIN, LOSE, or can drag our prefab "AdsOnStart" on second scene game map
 - Button More Games you can add only on main scene for game if want or else without.

Example:

public class ExampleClass: MonoBehaviour {
	void Awake()
	{
	  GameMonetize.OnResumeGame += ResumeGame;
	  GameMonetize.OnPauseGame += PauseGame;
	}
	
	void OnDestroy()
	{
	  GameMonetize.OnResumeGame -= ResumeGame;
	  GameMonetize.OnPauseGame -= PauseGame;
	}

	public void ResumeGame()
	{
	  // RESUME MY GAME
	    AudioListener.volume = 1f;
            Time.timeScale = 1f;
	}

	public void PauseGame()
	{
	  // PAUSE MY GAME
	   Time.timeScale = 0.01f;
           AudioListener.volume = 0f;
	}

	public void ShowAd()
	{
	  GameMonetize.Instance.ShowAd();	
	}
}

Unity Typs Tutorials - please read to optimize your game
- Compress Audio - https://gamemonetize.com/blog/optimize-your-unity-game-size-with-efficient-audio-compression
- Compress FBX - https://gamemonetize.com/blog/improving-game-performance-how-to-reduce-fbx-file-sizes-in-unity
- Compress Image - https://gamemonetize.com/blog/unlocking-better-performance-in-unity-games-through-image-optimization
- Template Webgl - https://gamemonetize.com/blog/the-complete-guide-to-unity-webgl-templates-and-customization
- Make your own website free Crazy Arcade CMS https://gamemonetize.com/cms , we update all time this cool template for free