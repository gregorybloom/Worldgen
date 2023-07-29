using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
//using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;


[ExecuteAlways]
public class EngineManager : MonoBehaviour
{
    public enum EngineActionInputClass
    {
        START = 0,
        SELECT = 1
    }


    private static EngineManager instance = null;
    public static EngineManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (EngineManager)FindObjectOfType(typeof(EngineManager));
            }
            return instance;
        }
        private set
        {
            instance = value;
        }
    }

    public SpriteDatabase SpriteDatabase {  get; private set; }
    public GameControlManager GameControlManager { get; private set; }
    /*
    public SceneLookupManager SceneLookupManager { get; private set; }
    public SpriteRenderingManager SpriteRenderingManager { get; private set; }
    /*
    public MusicManager MusicManager { get; private set; }
    public ActorFactory ActorFactory { get; private set; }
    public ControlManager ControlManager { get; private set; }
    public LogManager LogManager { get; private set; }
    public ObjectLookupManager ObjectLookupManager { get; private set; }
    public SpriteManager SpriteManager { get; private set; }
    public SerializationManager SerializationManager { get; private set; }
    public SFXManager SFXManager { get; private set; }
    public UtilsManager UtilsManager { get; private set; }
    public NetworkSessionManager NetworkSessionManager { get; private set; }
    /*
    public EditorManager EditorManager { get; private set; }
    public MenuManager MenuManager { get; private set; }
    public SessionManager SessionManager { get; private set; }
    public WorldManager WorldManager { get; private set; }
    public GridManager GridManager { get; private set; }
    /**/

    public bool loaded = false;

    /*
    public GameSessionManager gameSession = null;
    public PlayerAgentManager playerAgentManager = null;
    public EngineTestManager engineTestManager = null;
    /**/

    public bool appHasFocus = true;
    public bool appHasPause = true;

    public enum EnginePlayState { DebugGamePlay, EditorGamePlay, NormalGamePlay }
    public EnginePlayState engineState = EnginePlayState.NormalGamePlay;
    public enum EngineType { Unity, Build }
    public EngineType engineType = EngineType.Build;
    public enum EngineMode { UnityEditorMode, UnityPlayingMode }
    public EngineMode engineMode = EngineMode.UnityEditorMode;


    public enum GameEditorArrowMode { None, Music, SFX }
    public GameEditorArrowMode arrowSelectorMode = GameEditorArrowMode.None;


    public enum EngineTimerClass
    {
        GAMEPLAY = 0,
        GAMEUI = 1,
        ENGINE = 2
    }
    private int timersCount = 3;
    public List<EngineTimer> timersList = new List<EngineTimer>();



    [SerializeField]
    public string gameType = "";
    private void Awake()
    {
        if (Instance != null && Instance != this && Application.isPlaying)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }


        for (int i = 0; i < timersCount; i++)
        {
            timersList.Add(new EngineTimer());
        }


        engineType = EngineType.Build;
        if (Application.isEditor)
        {
            engineType = EngineType.Unity;
        }

        engineMode = EngineMode.UnityEditorMode;
        if (Application.isPlaying)
        {
            engineMode = EngineMode.UnityPlayingMode;
        }


        if (engineMode == EngineMode.UnityPlayingMode)
        {
            DontDestroyOnLoad(instance.gameObject);
        }


#if UNITY_EDITOR

#else

#endif
        SpriteDatabase = GetComponent<SpriteDatabase>();
        GameControlManager = GetComponent<GameControlManager>();
        /*
        SceneLookupManager = GetComponent<SceneLookupManager>();
        SpriteRenderingManager = GetComponent<SpriteRenderingManager>();
        /*
        MusicManager = GetComponent<MusicManager>();
        SpriteManager = GetComponent<SpriteManager>();
        ActorFactory = GetComponent<ActorFactory>();
        ControlManager = GetComponent<ControlManager>();
        ObjectLookupManager = GetComponent<ObjectLookupManager>();
        SerializationManager = GetComponent<SerializationManager>();
        SFXManager = GetComponent<SFXManager>();
        LogManager = GetComponent<LogManager>();
        UtilsManager = GetComponent<UtilsManager>();
        NetworkSessionManager = GetComponent<NetworkSessionManager>();

        /*
                EditorManager = GetComponent<EditorManager>();
                GridManager = GetComponent<GridManager>();
                MenuManager = GetComponent<MenuManager>();
                WorldManager = GetComponent<WorldManager>();
                SessionManager = GetComponent<SessionManager>();
                /**/






        SceneManager.sceneLoaded += OnSceneLoaded;
        if (!loaded)
        {
            if (engineMode == EngineMode.UnityPlayingMode)
            {

//                if (!SceneManager.GetSceneByBuildIndex(1).isLoaded)
//                {
#if UNITY_EDITOR
                    /*
                    TestScenarioManager.ImportScenarioArgs();
                    if (TestScenarioManager.testScenarioActive)
                    {
                        TestScenarioManager.InitializeSceneLoadingCallback();
                        TestScenarioManager.InitializeTestingScenario();
                    }
                    else
                    {
                    /**/
//                    SceneManager.LoadScene(EditorBuildSettings.scenes[1].path, LoadSceneMode.Single);
                    //                    }
#else
//                    UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/LevelScene", LoadSceneMode.Single);
#endif
//                }
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded: " + scene.name);

        if (scene.name == "GameScene")
        {
            Debug.Log("Begin Component Initialization");
            //            ActorFactory.loadPrefabs();
            if (engineMode == EngineMode.UnityEditorMode)
            {
                /*
                EditorManager.setup();
                /**/
            }
            SceneManager.SetActiveScene(scene);
            loaded = true;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        if (engineMode == EngineMode.UnityPlayingMode)
        {

        }

        for (int i = 0; i < timersList.Count; i++)
        {
            timersList[i].StartTimer();
        }
    }


    public void StartUIEditor()
    {
        if (engineMode != EngineMode.UnityEditorMode) return;
    }

    public void ReadInput()
    {
        if (engineMode == EngineMode.UnityPlayingMode && engineType == EngineType.Unity)
        {
            /*
            if (Keyboard.current.shiftKey.isPressed && Keyboard.current.ctrlKey.isPressed)
            {
                if (Keyboard.current.FindKeyOnCurrentKeyboardLayout("q").wasPressedThisFrame) SwapEngineStateTo(EnginePlayState.DebugGamePlay);
                if (Keyboard.current.FindKeyOnCurrentKeyboardLayout("w").wasPressedThisFrame) SwapEngineStateTo(EnginePlayState.NormalGamePlay);
                if (Keyboard.current.FindKeyOnCurrentKeyboardLayout("e").wasPressedThisFrame) SwapEngineStateTo(EnginePlayState.EditorGamePlay);
                if (Keyboard.current.backslashKey.wasPressedThisFrame) MusicManager.ToggleMuteVolume();
                if (Keyboard.current.FindKeyOnCurrentKeyboardLayout("m").wasPressedThisFrame)
                {
                    if (arrowSelectorMode != GameEditorArrowMode.Music) arrowSelectorMode = GameEditorArrowMode.Music;
                    else if (arrowSelectorMode == GameEditorArrowMode.Music) arrowSelectorMode = GameEditorArrowMode.None;
                }
                if (Keyboard.current.slashKey.wasPressedThisFrame)
                {
                    if (arrowSelectorMode != GameEditorArrowMode.SFX) arrowSelectorMode = GameEditorArrowMode.SFX;
                    else if (arrowSelectorMode == GameEditorArrowMode.SFX) arrowSelectorMode = GameEditorArrowMode.None;
                }
                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    if (arrowSelectorMode == GameEditorArrowMode.Music) MusicManager.ChangeAudioVolume(MusicManager.ChangeVolumeType.Music, true);
                    else if (arrowSelectorMode == GameEditorArrowMode.SFX) MusicManager.ChangeAudioVolume(MusicManager.ChangeVolumeType.Global, true);
                }
                if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    if (arrowSelectorMode == GameEditorArrowMode.Music) MusicManager.ChangeAudioVolume(MusicManager.ChangeVolumeType.Music, false);
                    else if (arrowSelectorMode == GameEditorArrowMode.SFX) MusicManager.ChangeAudioVolume(MusicManager.ChangeVolumeType.Global, false);
                }
            }
            /**/
        }
    }
    public void SwapEngineStateTo(EnginePlayState v)
    {
        if (engineState == v) return;
        Debug.Log("Change State to: " + JsonUtility.ToJson(v));
        EnginePlayState lastState = engineState;
        engineState = v;

        if (v == EnginePlayState.EditorGamePlay)
        {
        }

    }

    private void OnApplicationFocus(bool focus)
    {
        appHasFocus = focus;
    }
    private void OnApplicationPause(bool pause)
    {
        appHasPause = pause;
    }


    /*
    public void ManagePlayerInput(InputAction.CallbackContext context)
    {
        PlayerInput inputController = GetComponent<PlayerInput>();

        if (inputController.currentActionMap == null) return;
        if (inputController.currentActionMap.ToString().EndsWith(":Player"))
        {
            Debug.Log(context.control.name);
            Debug.Log(context.control.path);

            if (context.action.ToString().Contains("Player/START[") || context.action.ToString().Contains("BasicUI/START["))
            {
//                DistributeEngineActionInput(context, EngineManager.EngineActionInputClass.START);
            }
            else if (context.action.ToString().Contains("Player/SELECT[") || context.action.ToString().Contains("BasicUI/SELECT["))
            {
//                DistributeEngineActionInput(context, EngineManager.EngineActionInputClass.SELECT);
            }
        }
    }
    /**/

    //    public void DistributeEngineActionInput(InputAction.CallbackContext context, EngineManager.EngineActionInputClass actionInputClass, PlayerInterface playerInterface)
    /*
    public void DistributeEngineActionInput(InputAction.CallbackContext context, EngineManager.EngineActionInputClass actionInputClass)
    {
        List<IReceiveEngineActionInput> tempList = new List<IReceiveEngineActionInput>(GetComponentsInChildren<IReceiveEngineActionInput>().Where(
            interfaceItem => true
            ).ToArray());

        for (int i = 0; i < tempList.Count; i++)
        {
            //            tempList[i].ReceiveEngineActionInput(context, actionInputClass, playerInterface);
            tempList[i].ReceiveEngineActionInput(context, actionInputClass);
        }
    }
    /**/

    void Update()
    {
        for (int i = 0; i < timersList.Count; i++)
        {
            timersList[i].UpdateTimer();
        }

        ReadInput();
    }



    public EngineTimer GetEngineTimer(EngineTimerClass timer)
    {
        if ((int)timer < timersList.Count)
        {
            return timersList[(int)timer];
        }
        return null;
    }
    public float GetGameTime(EngineTimerClass timer, bool fixedTimer, bool delta)
    {
        if ((int)timer < timersList.Count)
        {
            if (delta) return GetEngineTimer(timer).GetTimeFrameDiff(fixedTimer);
            else return GetEngineTimer(timer).GetCurrentTime();
        }
        return -1f;
    }

}


/*
public interface IReceiveEngineActionInput
{
    //    public void ReceiveEngineActionInput(InputAction.CallbackContext context, EngineManager.EngineActionInputClass actionClass, PlayerInterface playerInterface);
    public void ReceiveEngineActionInput(InputAction.CallbackContext context, EngineManager.EngineActionInputClass actionClass);

}
/**/
