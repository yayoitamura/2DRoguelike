using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Completed
{
	using System.Collections.Generic;		//Allows us to use Lists. 
	using UnityEngine.UI;					//Allows us to use UI.
	
    //GameManager
	public class GameManager : MonoBehaviour
	{
		public float levelStartDelay = 2f;                      //レベル開始前の待機時間
		public float turnDelay = 0.1f;                          //Delay between each Player turn.各プレイヤーのターンの間のディレイ。
		public int playerFoodPoints = 100;                      //プレーヤのゲーム開始時のfood points
		public static GameManager instance = null;              //Static instance of GameManager which allows it to be accessed by any other script.他のスクリプトがアクセスできるようにするGameManagerの静的インスタンス。
																//他のスクリプトがアクセスできるようにするGameManagerの静的インスタンス。
		[HideInInspector] public bool playersTurn = true;		//Boolean to check if it's players turn, hidden in inspector but public.
                                                                //ブール値で、プレイヤーが回っているか、インスペクターではなくパブリックになっているかを調べます。
		
		
		private Text levelText;                                 //Text to display current level number.現在のレベル番号を表示するテキスト。
		private GameObject levelImage;                          //Image to block out level as levels are being set up, background for levelText.レベルが設定されているのでレベルをブロックする画像、levelTextの背景。
		private BoardManager boardScript;                       //Store a reference to our BoardManager which will set up the level.レベルを設定するBoardManagerへの参照を保存する
		private int level = 1;                                  //Current level number, expressed in game as "Day 1".現在のレベル番号。ゲームでは「1日目」と表現されます。
		private List<Enemy> enemies;                            //List of all Enemy units, used to issue them move commands.移動コマンドを発行するために使用されるすべての敵ユニットのリスト。
		private bool enemiesMoving;                             //Boolean to check if enemies are moving.敵が動いているかどうかを調べるブール値。
		private bool doingSetup = true;                         //Boolean to check if we're setting up board, prevent Player from moving during setup.
																//ボードをセットアップしているかどうかを確認するブール値。セットアップ中にPlayerが移動しないようにします。



		//Awake is always called before any Start functions   Awakeは常にStart関数の前に呼び出されます
		void Awake()
		{
			//Check if instance already exists インスタンスがすでに存在するかどうかを確認する
			if (instance == null)

				//if not, set instance to this そうでない場合は、instanceをthisに設定します。
				instance = this;

			//If instance already exists and it's not this: インスタンスが既に存在し、それがこれでない場合：
			else if (instance != this)

                //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
                //その後これを破壊する。 これにより、シングルトンのパターンが強制されます。つまり、GameManagerのインスタンスは1つしか存在できません。
                Destroy(gameObject);

			//Sets this to not be destroyed when reloading scene シーンをリロードするときに破棄しないように設定します
			DontDestroyOnLoad(gameObject);

			//Assign enemies to a new List of Enemy objects. 敵を新しい敵のリストオブジェクトに割り当てます。
			enemies = new List<Enemy>();

			//Get a component reference to the attached BoardManager script 付属のBoardManagerスクリプトへのコンポーネント参照を取得する
			boardScript = GetComponent<BoardManager>();

			//Call the InitGame function to initialize the first level   InitGame関数を呼び出して最初のレベルを初期化する
			InitGame();
		}

        //this is called only once, and the paramter tell it to be called only after the scene was loaded
        //(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
        //これは一度だけ呼び出され、シーンがロードされた後に呼び出されるようにパラメータに指示します（そうでなければ、シーンロードコールバックは最初のロードと呼ばれ、
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static public void CallbackInitialization()
        {
			//register the callback to be called everytime the scene is loaded シーンがロードされるたびに呼び出されるコールバックを登録する
			SceneManager.sceneLoaded += OnSceneLoaded;
        }

		//This is called each time a scene is loaded. これはシーンがロードされるたびに呼び出されます。
		static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            instance.level++;
            instance.InitGame();
        }


		//Initializes the game for each level. 各レベルのゲームを初期化します。
		void InitGame()
		{
			//While doingSetup is true the player can't move, prevent player from moving while title card is up.
			doingSetup = true;
			
			//Get a reference to our image LevelImage by finding it by name.
			levelImage = GameObject.Find("LevelImage");
			
			//Get a reference to our text LevelText's text component by finding it by name and calling GetComponent.
			levelText = GameObject.Find("LevelText").GetComponent<Text>();
			
			//Set the text of levelText to the string "Day" and append the current level number.
			levelText.text = "Day " + level;
			
			//Set levelImage to active blocking player's view of the game board during setup.
			levelImage.SetActive(true);
			
			//Call the HideLevelImage function with a delay in seconds of levelStartDelay.
			Invoke("HideLevelImage", levelStartDelay);
			
			//Clear any Enemy objects in our List to prepare for next level.
			enemies.Clear();
			
			//Call the SetupScene function of the BoardManager script, pass it current level number.
			boardScript.SetupScene(level);
			
		}


		//Hides black image used between levels レベル間で使用される黒い画像を非表示にする
		void HideLevelImage()
		{
			//Disable the levelImage gameObject.
			levelImage.SetActive(false);
			
			//Set doingSetup to false allowing player to move again.
			doingSetup = false;
		}

		//Update is called every frame. 更新はすべてのフレームと呼ばれます。
		void Update()
		{
			//Check that playersTurn or enemiesMoving or doingSetup are not currently true.
			if(playersTurn || enemiesMoving || doingSetup)
				
				//If any of these are true, return and do not start MoveEnemies.
				return;
			
			//Start moving enemies.
			StartCoroutine (MoveEnemies ());
		}
		
		//Call this to add the passed in Enemy to the List of Enemy objects.
        //これを呼び出して、渡された敵を敵のリストオブジェクトに追加します。
		public void AddEnemyToList(Enemy script)
		{
			//Add Enemy to List enemies.
			enemies.Add(script);
		}


		//GameOver is called when the player reaches 0 food points   GameOverは、プレイヤーが0点の食べ物ポイントに達すると呼び出されます
		public void GameOver()
		{
			//Set levelText to display number of levels passed and game over message
			levelText.text = "After " + level + " days, you starved.";
			
			//Enable black background image gameObject.
			levelImage.SetActive(true);
			
			//Disable this GameManager.
			enabled = false;
		}

		//Coroutine to move enemies in sequence. コルーチンは順番に敵を動かす。
		IEnumerator MoveEnemies()
		{
			//While enemiesMoving is true player is unable to move.
			enemiesMoving = true;
			
			//Wait for turnDelay seconds, defaults to .1 (100 ms).
			yield return new WaitForSeconds(turnDelay);
			
			//If there are no enemies spawned (IE in first level):
			if (enemies.Count == 0) 
			{
				//Wait for turnDelay seconds between moves, replaces delay caused by enemies moving when there are none.
				yield return new WaitForSeconds(turnDelay);
			}
			
			//Loop through List of Enemy objects.
			for (int i = 0; i < enemies.Count; i++)
			{
				//Call the MoveEnemy function of Enemy at index i in the enemies List.
				enemies[i].MoveEnemy ();
				
				//Wait for Enemy's moveTime before moving next Enemy, 
				yield return new WaitForSeconds(enemies[i].moveTime);
			}
			//Once Enemies are done moving, set playersTurn to true so player can move.
			playersTurn = true;
			
			//Enemies are done moving, set enemiesMoving to false.
			enemiesMoving = false;
		}
	}
}

