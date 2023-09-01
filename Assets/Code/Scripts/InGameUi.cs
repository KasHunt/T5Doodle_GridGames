using System.Collections;
using Code.Scripts.Chess;
using TiltFive.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts
{
	public class InGameUi : MonoBehaviour
	{
		[Header("Common")]
		public AudioClip uiNavSound;
		
		[Header("HUD Icons")]
		public GameObject settingMenu;

		[Header("Pages")]
		public GameObject settingsPage;
		public GameObject newGamePage;
		public GameObject changeGamePage;
		public GameObject pawnPromotionPage;
		public GameObject helpPage;

		[Header("Games")]
		public GameObject chessGame;
		public GameObject reversiGame;
		public GameObject checkersGame;
		
		[Header("Setting Page UI Elements")]
		public GameObject soundEffectVolumeSlider;
		public GameObject musicVolumeSlider;
		public GameObject wandArcSlider;

		private Slider _soundEffectVolumeSlider;
		private Slider _musicVolumeSlider;
		private Slider _wandArcSlider;
		private bool _gameSelected;
		
		private float _currentRotation;
		private float _targetRotation;
		
		// Static instance of UI, accessible from anywhere
		public static InGameUi Instance { get; private set; }

		private void Awake()
		{
			// Check for existing instances and destroy this instance if we've already got one
			if (Instance != null && Instance != this)
			{
				Log.Warn("Destroying duplicate InGameUi");
				Destroy(gameObject);
				return;
			}

			// Set this instance as the Singleton instance
			Instance = this;
			
			// Persist across scenes
			DontDestroyOnLoad(gameObject);
		}

		private void Start()
		{
			// Get UI element handles
			_soundEffectVolumeSlider = soundEffectVolumeSlider.GetComponent<Slider>();
			_musicVolumeSlider = musicVolumeSlider.GetComponent<Slider>();
			_wandArcSlider = wandArcSlider.GetComponent<Slider>();
			
			// Set the initial slider values
			_soundEffectVolumeSlider.value = SoundManager.Instance.EffectVolume;
			_musicVolumeSlider.value = SoundManager.Instance.MusicVolume;
			_wandArcSlider.value = WandManager.Instance.arcLaunchVelocity;
			
			// Show the help screen on startup
			ToggleHelpScreen();
		}

		private void PlayUiNavSound()
		{
			SoundManager.Instance.PlaySound(uiNavSound, 1);
		}

		public void SetMusicVolume(float value)
		{
			SoundManager.Instance.MusicVolume = value;
		}
		
		public void SetSoundEffectsVolume(float value)
		{
			SoundManager.Instance.EffectVolume = value;
		}
		
		public void SetWandArcVelocity(float value)
		{
			WandManager.Instance.arcLaunchVelocity = value;
		}

		public void ToggleSettingsMenu()
		{
			PlayUiNavSound();
			settingMenu.SetActive(!settingMenu.activeSelf);
		}

		public void ToggleHelpScreen()
		{
			PlayUiNavSound();
			var show = !helpPage.activeSelf;
			helpPage.SetActive(show);

			// On dismissing the help screen, show the game
			// picker if no game is selected
			if (!show && !_gameSelected) ShowChangeGameScreen();
		}
		
		public void ShowPawnPromotionScreen()
		{
			PlayUiNavSound();
			pawnPromotionPage.SetActive(true);
		}

		public void PromotePawnToQueen()
		{
			PlayUiNavSound();
			pawnPromotionPage.SetActive(false);
			Chess.Chess.Instance.PromotePawn(PieceType.Queen);
		}
		
		public void PromotePawnToRook()
		{
			PlayUiNavSound();
			pawnPromotionPage.SetActive(false);
			Chess.Chess.Instance.PromotePawn(PieceType.Rook);
		}
		
		public void PromotePawnToKnight()
		{
			PlayUiNavSound();
			pawnPromotionPage.SetActive(false);
			Chess.Chess.Instance.PromotePawn(PieceType.Knight);
		}
		
		public void PromotePawnToBishop()
		{
			PlayUiNavSound();
			pawnPromotionPage.SetActive(false);
			Chess.Chess.Instance.PromotePawn(PieceType.Bishop);
		}
		
		public void ShowNewGamesScreen()
		{
			PlayUiNavSound();
			settingMenu.SetActive(false);
			newGamePage.SetActive(true);
		}
		
		public void HideNewGamesScreen()
		{
			PlayUiNavSound();
			newGamePage.SetActive(false);
		}
		
		public void ShowSettingsScreen()
		{
			PlayUiNavSound();
			settingMenu.SetActive(false);
			settingsPage.SetActive(true);
		}
		
		public void HideSettingsScreen()
		{
			PlayUiNavSound();
			settingsPage.SetActive(false);
		}

		public void ShowChangeGameScreen()
		{
			PlayUiNavSound();
			settingMenu.SetActive(false);
			changeGamePage.SetActive(true);
		}
		
		public void SwitchGameChess()
		{
			PlayUiNavSound();
			changeGamePage.SetActive(false);
			chessGame.SetActive(true);
			reversiGame.SetActive(false);
			checkersGame.SetActive(false);
			_gameSelected = true;
		}
		
		public void SwitchGameReversi()
		{
			PlayUiNavSound();
			changeGamePage.SetActive(false);
			chessGame.SetActive(false);
			reversiGame.SetActive(true);
			checkersGame.SetActive(false);
			_gameSelected = true;
		}
		
		public void SwitchGameCheckers()
		{
			PlayUiNavSound();
			changeGamePage.SetActive(false);
			chessGame.SetActive(false);
			reversiGame.SetActive(false);
			checkersGame.SetActive(true);
			_gameSelected = true;
		}
		
		public void NewGame()
		{
			var chessInstance = Chess.Chess.Instance;
			if (chessInstance && chessInstance.isActiveAndEnabled) chessInstance.NewGame();

			var reversiInstance = Reversi.Reversi.Instance;
			if (reversiInstance && reversiInstance.isActiveAndEnabled) reversiInstance.NewGame();
			
			var checkersInstance = Checkers.Checkers.Instance;
			if (checkersInstance && checkersInstance.isActiveAndEnabled) checkersInstance.NewGame();
			
			newGamePage.SetActive(false);
		}
		
		public void Quit()
		{
			PlayUiNavSound();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#else
	        Application.Quit();
#endif
		}

		private static void PopUI()
		{
			GameboardCanvas.Instance.PopCanvas();			
		}
	}
}