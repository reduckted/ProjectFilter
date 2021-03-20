using Microsoft.VisualStudio.PlatformUI;
using Moq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Xunit;


namespace ProjectFilter.UI.AttachedProperties {

    public class CheckBoxToggleTests {

        [WpfFact]
        public void TogglesIsCheckedPropertyWhenSpaceKeyIsPressed() {
            ListViewItem item;
            ViewModel vm;
            KeyEventArgs args;


            item = new ListViewItem();
            vm = new ViewModel();

            Setup(item, vm);

            Assert.False(vm.IsChecked);

            args = SendKeyDown(item, Key.Space);
            Assert.True(args.Handled);
            Assert.False(vm.IsChecked);

            args = SendKeyUp(item, Key.Space);
            Assert.True(args.Handled);
            Assert.True(vm.IsChecked);

            args = SendKeyDown(item, Key.Space);
            Assert.True(args.Handled);
            Assert.True(vm.IsChecked);

            args = SendKeyUp(item, Key.Space);
            Assert.True(args.Handled);
            Assert.False(vm.IsChecked);
        }


        [WpfFact]
        public void DoesNotTogglePropertyWhenOtherKeyIsPressed() {
            ListViewItem item;
            ViewModel vm;
            KeyboardEventArgs args;


            item = new ListViewItem();
            vm = new ViewModel();

            Setup(item, vm);

            Assert.False(vm.IsChecked);

            args = SendKeyDown(item, Key.Return);
            Assert.False(args.Handled);

            args = SendKeyUp(item, Key.Return);
            Assert.False(args.Handled);

            Assert.False(vm.IsChecked);
        }


        [WpfFact]
        public void DoesNotHandleAltSpace() {
            ListViewItem item;
            ViewModel vm;
            KeyboardEventArgs args;


            item = new ListViewItem();
            vm = new ViewModel();

            Setup(item, vm);

            Assert.False(vm.IsChecked);

            args = SendKeyDown(item, Key.Space, modifiers: ModifierKeys.Alt);
            Assert.False(args.Handled);

            args = SendKeyUp(item, Key.Space);
            Assert.False(args.Handled);

            Assert.False(vm.IsChecked);
        }


        [WpfFact]
        public void DoesNotToggleWhenElementLosesFocusBeforeKeyUp() {
            ListViewItem item;
            ViewModel vm;
            KeyboardEventArgs args;


            item = new ListViewItem();
            vm = new ViewModel();

            Setup(item, vm);

            args = SendKeyDown(item, Key.Space);
            Assert.True(args.Handled);

            SendKeyboardFocus(item, UIElement.LostKeyboardFocusEvent);

            args = SendKeyUp(item, Key.Space);
            Assert.False(args.Handled);

            Assert.False(vm.IsChecked);
        }


        [WpfFact]
        public void DoesNotToggleWhenElementGainsFocusBeforeKeyUp() {
            ListViewItem item;
            ViewModel vm;
            KeyboardEventArgs args;


            item = new ListViewItem();
            vm = new ViewModel();

            Setup(item, vm);

            SendKeyboardFocus(item, UIElement.GotKeyboardFocusEvent);

            args = SendKeyUp(item, Key.Space);
            Assert.False(args.Handled);

            Assert.False(vm.IsChecked);
        }


        [WpfFact]
        public void StopsTogglingWhenPropertyIsDisabled() {
            ListViewItem item;
            ViewModel vm;


            item = new ListViewItem();
            vm = new ViewModel();

            Setup(item, vm);

            Assert.False(vm.IsChecked);

            SendKeyDown(item, Key.Space);
            SendKeyUp(item, Key.Space);
            Assert.True(vm.IsChecked);

            CheckBoxToggle.SetEnabled(item, false);

            SendKeyDown(item, Key.Space);
            SendKeyUp(item, Key.Space);
            Assert.True(vm.IsChecked);
        }



        private static void Setup(FrameworkElement element, ViewModel viewModel) {
            Binding binding;


            CheckBoxToggle.SetEnabled(element, true);

            binding = new Binding(nameof(ViewModel.IsChecked)) {
                Source = viewModel
            };

            element.SetBinding(CheckBoxToggle.IsCheckedProperty, binding);
        }


        private static KeyEventArgs SendKeyDown(UIElement element, Key key, ModifierKeys modifiers = ModifierKeys.None) {
            return SendKey(element, key, modifiers, UIElement.KeyDownEvent);
        }


        private static KeyEventArgs SendKeyUp(UIElement element, Key key) {
            return SendKey(element, key, ModifierKeys.None, UIElement.KeyUpEvent);
        }


        private static KeyEventArgs SendKey(UIElement element, Key key, ModifierKeys modifiers, RoutedEvent keyEvent) {
            KeyEventArgs args;


            args = new KeyEventArgs(new TestKeyboard(modifiers), Mock.Of<PresentationSource>(), 0, key) {
                RoutedEvent = keyEvent
            };

            element.RaiseEvent(args);

            return args;
        }


        private static void SendKeyboardFocus(UIElement element, RoutedEvent focusEvent) {
            KeyboardFocusChangedEventArgs args;


            args = new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, element, null) {
                RoutedEvent = focusEvent
            };

            element.RaiseEvent(args);
        }


        private class ViewModel : ObservableObject {

            private bool _isChecked;


            public bool IsChecked {
                get { return _isChecked; }
                set { SetProperty(ref _isChecked, value); }
            }

        }


        private class TestKeyboard : KeyboardDevice {

            private readonly ModifierKeys _modifiers;


            public TestKeyboard(ModifierKeys modifiers) : base(InputManager.Current) {
                _modifiers = modifiers;
            }


            protected override KeyStates GetKeyStatesFromSystem(Key key) {
                bool pressed;


                switch (key) {
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        pressed = (_modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                        break;

                    case Key.LeftAlt:
                    case Key.RightAlt:
                        pressed = (_modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
                        break;

                    case Key.LeftShift:
                    case Key.RightShift:
                        pressed = (_modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                        break;

                    default:
                        pressed = false;
                        break;

                }

                return pressed ? KeyStates.Down : KeyStates.None;
            }

        }

    }

}
