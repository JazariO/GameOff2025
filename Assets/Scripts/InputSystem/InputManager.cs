using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Proselyte.Sigils;

public class InputManager : MonoBehaviour /*, ISaveableSettings*/
{
    private static bool isActive;

    [Header("Outgoing Variables")]

    [SerializeField] PlayerInputDataSO playerInputDataSO;

    [Header("Game Events SEND")]
    [SerializeField] GameEvent OnPauseInputEvent;
    [SerializeField] GameEvent OnQuickLoadInputEvent;
    [SerializeField] GameEvent OnQuickSaveInputEvent;

    [Header("Runtime Sets")]
    //[SerializeField] RebindableRuntimeSet rebindableRuntimeSet;
    //[SerializeField] SaveableSettingsRuntimeSet saveableSettingsRuntimeSet;

    [Header("References")]
    [SerializeField] InputActionAsset actions;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] StringVariable InputControlScheme;
    
    private bool _pauseHandledThisFrame;

    private InputActionMap _playerMap;
    private InputActionMap _uiMap;

    private InputAction _moveAction;
    private InputAction _interactAction;
    private InputAction _lookAction;
    private InputAction _pauseAction;
    private InputAction _changeViewAction;

    private void Awake()
    {
        if(!InputManager.isActive)
            InputManager.isActive = true;
        else
            return;

        // Set initial defaults
        playerInputDataSO.input_move = Vector2.zero;
        playerInputDataSO.input_look = Vector2.zero;
        playerInputDataSO.input_interact = false;
        playerInputDataSO.input_change_view = false;
        playerInputDataSO.input_mouse_position = Vector2.zero;
        playerInputDataSO.input_mouse_button_left = false;
        playerInputDataSO.input_mouse_button_right = false;

        // Init action maps
        _playerMap = playerInput.actions.FindActionMap("Player", true);
        _uiMap = playerInput.actions.FindActionMap("UI", true);

        // Init player actions
        _moveAction = playerInput.actions["Move"];
        _interactAction = playerInput.actions["Interact"];
        _lookAction = playerInput.actions["Look"];
        _pauseAction = playerInput.actions["Pause"];
        _changeViewAction = playerInput.actions["ChangeView"];
    }

    private void OnEnable()
    {
        if(!InputManager.isActive)
        {
            gameObject.SetActive(false);
            return;
        }

        // Init Player Prefs (if any)
        var rebinds = PlayerPrefs.GetString("rebinds");
        if(!string.IsNullOrEmpty(rebinds))
        {
            actions.LoadBindingOverridesFromJson(rebinds);
            //UpdateAllBindingDisplays();
        }

        if(_playerMap == null && _uiMap == null) return;
        _playerMap.Enable();
        _uiMap.Disable();

        InputSystem.onActionChange += OnInputActionChanged;
        playerInput.onControlsChanged += OnPlayerInputControlsChanged;

        // TODO(Jazz): Do we need a RebindsManager for all this?
        //saveableSettingsRuntimeSet.Add(this);
    }

    private void OnDisable()
    {
        if(InputManager.isActive) return;

        // Save current binds to player prefs
        var rebinds = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);

        if(_playerMap == null && _uiMap == null) return;
        _uiMap.Disable();
        _playerMap.Disable();

        InputSystem.onActionChange -= OnInputActionChanged;

        //saveableSettingsRuntimeSet.Remove(this);
    }

    private void Start()
    {
        if(InputManager.isActive) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        playerInputDataSO.input_move = _moveAction.ReadValue<Vector2>();
        playerInputDataSO.input_look = _lookAction.ReadValue<Vector2>();
        playerInputDataSO.input_interact = _interactAction.WasPressedThisFrame();
        playerInputDataSO.input_change_view = _changeViewAction.WasPressedThisFrame();

        playerInputDataSO.input_mouse_position = Mouse.current.position.ReadValue();
        playerInputDataSO.input_mouse_button_left = Mouse.current.leftButton.wasPressedThisFrame;
        playerInputDataSO.input_mouse_button_right = Mouse.current.rightButton.wasPressedThisFrame;

        if(_pauseAction.WasPressedThisFrame() && !_pauseHandledThisFrame)
        {
            OnPauseInputEvent.Raise();
            _pauseHandledThisFrame = true;
        }
    }

    public void OnPlayGameEventRaised()
    {
        _uiMap.Disable();
        _playerMap.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnPauseGameEventRaised()
    {
        _playerMap.Disable();
        _uiMap.Enable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Ensures input visuals stay accurate when bindings change 
    // including external changes like keyboard layout shifts.
    private void OnInputActionChanged(object obj, InputActionChange inputActionChange)
    {
        if(inputActionChange != InputActionChange.BoundControlsChanged)
        {
            return;
        }

        //UpdateAllBindingDisplays();
    }

    // Passes its value to all rebindables for glyph control scheme detection.
    private void OnPlayerInputControlsChanged(PlayerInput playerInput)
    {
        InputControlScheme.value = playerInput.currentControlScheme;
    }

    //public void UpdateAllBindingDisplays()
    //{
    //    // NOTE(Jazz): Just completed a rebind, need to update visuals for all binding displays in the ui
    //    foreach(var rebindable in rebindableRuntimeSet.Items)
    //    {
    //        rebindable.UpdateBindingDisplay();
    //    }
    //}

    //public void ResetAllBindingOverrides()
    //{
    //    foreach(var rebindable in rebindableRuntimeSet.Items)
    //    {
    //        rebindable.ResetBinding();
    //    }
    //}

    private void LateUpdate()
    {
        _pauseHandledThisFrame = false;
    }

    //void ISaveableSettings.PopulateUserSettingsData()
    //{
    //    var rebinds = actions.SaveBindingOverridesAsJson();
    //    PlayerPrefs.SetString("rebinds", rebinds);
    //}

    //void ISaveableSettings.ApplyUserSettingsData()
    //{
    //    var rebinds = PlayerPrefs.GetString("rebinds");
    //    if(!string.IsNullOrEmpty(rebinds))
    //    {
    //        actions.LoadBindingOverridesFromJson(rebinds);
    //        UpdateAllBindingDisplays();
    //    }
    //}
}
