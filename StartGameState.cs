using Code.GameLogic.Configs;
using Code.Input;
using System.Collections.Generic;
using System.Linq;
using Code.GameLogic.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Code.Input.PlayerInput;

namespace Code.States
{
    public class StartGameState : MonoBehaviourState
    {
        [SerializeField] private GameModeSelectButton _gameModeSelectButtonPrefab;
        [SerializeField] private Transform _buttonsContainer;
        [SerializeField] private GameModeInfoConfig _gameModeInfoConfig;
        [SerializeField] private GamepadDrivenButton _exit;
        [SerializeField] private GamepadDrivenButton _settings;
        [SerializeField] private GamepadDrivenButton _leaderboard;

        private IReadOnlyList<PlayerInput> _input;
        private List<InputDevice> _devices;
        private ExtendedGamepadSelector _gamepadButtonSelector;
        private List<GamepadDrivenButton> _buttons;
        public override void Enter(StateMachine machine, bool returned = false)
        {
            if(_animator)
                _animator.Play(returned?"Return":"Push", 0);

            if (_machine == null) 
                _machine = machine;
            
            _machine.GameData.ResetPlayerData();
            
            InstantiateButtons();

            if(gameObject.activeSelf == false)
                gameObject.SetActive(true);

            _input = _machine.Input.Controllers;
            _exit.Init(ExitPopUp, "");
            _settings.Init(OpenSettings, "");
            _leaderboard.Init(OpenLeaderboard, "");
        }

        public override void InitVoiceCommands()
        {
            _machine.VoiceInputWrapper.ClearActions();
            _machine.VoiceInputWrapper.BackCommand = ExitPopUp;
            _machine.VoiceInputWrapper.OpenSettingsCommand = OpenSettings;
            _machine.VoiceInputWrapper.OpenLeaderboardCommand = OpenLeaderboard;
            _machine.VoiceInputWrapper.ToGameModeCommand  = gamemode => { StartPlay((Constants.GameMode)gamemode);};
        }

        public override void Exit(bool returned = false)
        {
            if (_animator)
                _animator.Play(returned?"Back":"Pop", 0);
            //gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_gamepadButtonSelector == null)
            {
                SetGamepad(_input.Where(c => c.UseGamepad).Select(s => s.BindedGamepad).ToList());
            }

            if (!_machine.Input.Locked)
            {
                _gamepadButtonSelector?.Update();

                if (_input.Any(ct => ct.IsPause(true)))
                {
                    ExitPopUp();
                }
            }
        }

        private void InstantiateButtons()
        {
            _buttons = new List<GamepadDrivenButton>();

            foreach (Transform child in _buttonsContainer)
                Destroy(child.gameObject);

            foreach (var gameModeInfo in _gameModeInfoConfig.Infos)
            {
                if(gameModeInfo.Difficult == GameModeDifficult.Veteran)
                    continue;
                
                var button = Instantiate(_gameModeSelectButtonPrefab, _buttonsContainer);
                button.Init(() => { StartPlay(gameModeInfo.GameMode); }, gameModeInfo);
                _buttons.Add(button.GetComponent<GamepadDrivenButton>());
            }
        }

        public override void SetGamepad(List<InputDevice> devices)
        {
            if (devices == null || devices.Count < 1)
                return;

            _devices = devices;
            
            List<ButtonsGroup> groups = new List<ButtonsGroup>
            {
                new ButtonsGroup(false, _exit, _settings, _leaderboard),
                new ButtonsGroup(false, _buttons.ToArray())
            };
            _gamepadButtonSelector = new ExtendedGamepadSelector(_devices, groups, false);
        }

        private void StartPlay(Constants.GameMode gameMode)
        {
            _machine.Pop();                
            _machine.Push<GameModeState>();
            _machine.Peek<GameModeState>().SetState(gameMode, _machine.GameData.VeteranIsAvailable(gameMode));
        }

        private void OpenSettings()
        {
            _machine.Pop();                
            _machine.Push<SettingsState>();
            _machine.Peek<SettingsState>().SetGamepad(_machine.Input.Controllers.Where(c => c.UseGamepad).Select(s => s.BindedGamepad).ToList());
        }

        private void OpenLeaderboard()
        {
            _machine.Pop();                
            _machine.Push<LeaderboardState>();
            _machine.Peek<LeaderboardState>().SetGamepad(_machine.Input.Controllers.Where(c => c.UseGamepad).Select(s => s.BindedGamepad).ToList());
        }

        private void ExitPopUp()
        {
            _machine.Pop();
            _machine.Push<ConfirmPopupState>();
            _machine.Peek<ConfirmPopupState>().InitPopUp("Выйти из игры?", "Да", ExitApplication, "Нет", CancelExit);
        }

        private void CancelExit()
        {
            _machine.BackTo<StartGameState>();
        }

        private void ExitApplication()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}