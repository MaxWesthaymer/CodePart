using System;
using System.Collections.Generic;
using System.Linq;
using Code.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.States
{
    public class ConfirmPopupState : MonoBehaviourState
    {
        [SerializeField] private GamepadDrivenButton _confirm;
        [SerializeField] private GamepadDrivenButton _cancel;
        [SerializeField] private TextMeshProUGUI _dialogCaption;
        [SerializeField] private CanvasGroup _canvasGroup;
        private Action _onConfirm;
        private Action _onCancel;
        private ExtendedGamepadSelector _gamepadButtonSelector;
        private List<GamepadDrivenButton> _buttons;

        public override void Enter(StateMachine machine, bool returned = false)
        {
            if (_animator)
                _animator.Play("FadeIn", 0);

            if (_machine == null)
                _machine = machine;
        }

        public override void InitVoiceCommands()
        {
            _machine.VoiceInputWrapper.ClearActions();
            _machine.VoiceInputWrapper.BackCommand = Cancel;
            _machine.VoiceInputWrapper.ConfirmCommand = Confirm;
        }

        public void InitPopUp(string caption, string confirmCaption, Action onConfirm, string cancelCaption = null, Action onCancel = null)
        {
            _dialogCaption.text = caption.ToUpper();
            _onCancel = onCancel;
            _onConfirm = onConfirm;

            _buttons = new List<GamepadDrivenButton>();
            if (cancelCaption != null)
            {
                _cancel.gameObject.SetActive(true);
                _cancel.Init(Cancel, cancelCaption);
                _buttons.Add(_cancel);
            }

            _confirm.Init(Confirm, confirmCaption);
            _buttons.Add(_confirm);

            SetGamepad(_machine.Input.Controllers.Where(c=>c.UseGamepad).Select(s=>s.BindedGamepad).ToList());
        }

        public override void Exit(bool returned = false)
        {
            if (_animator)
                _animator.Play("FadeOut", 0);

            _canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if(!_machine.Input.Locked)
                _gamepadButtonSelector?.Update();

            UpdatePauseButton();
        }

        public override void SetGamepad(List<InputDevice> devices)
        {
            if (devices == null || devices.Count < 1)
                return;

            List<ButtonsGroup> groups = new List<ButtonsGroup>
            {
                new ButtonsGroup(false, _buttons.ToArray())
            };
            _gamepadButtonSelector = new ExtendedGamepadSelector(devices, groups, false, 0, null, 0);
        }

        private void UpdatePauseButton()
        {
            if (_machine.Input.Controllers.Any(ct => ct.IsPause(true)))
            {
                Cancel();
            }
        }

        private void Cancel()
        {
            _machine.Pop();
            _onCancel?.Invoke();
        }

        private void Confirm()
        {
            _machine.Pop();
            _onConfirm?.Invoke();
        }
    }
}