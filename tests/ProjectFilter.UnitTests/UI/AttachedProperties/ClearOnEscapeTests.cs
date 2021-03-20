using Moq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xunit;


namespace ProjectFilter.UI.AttachedProperties {

    public class ClearOnEscapeTests {

        [WpfFact]
        public void ClearsTextAndHandledEventWhenTextIsNotEmpty() {
            TextBox box;
            KeyEventArgs args;


            box = new TextBox { Text = "test" };

            ClearOnEscape.SetEnabled(box, true);
            args = SendKeyDown(box, Key.Escape);

            Assert.True(args.Handled);
            Assert.Equal("", box.Text);
        }


        [WpfFact]
        public void OnlyHandlesEscape() {
            TextBox box;
            KeyEventArgs args;


            box = new TextBox { Text = "test" };

            ClearOnEscape.SetEnabled(box, true);
            args = SendKeyDown(box, Key.Return);

            Assert.False(args.Handled);
            Assert.Equal("test", box.Text);
        }


        [WpfFact]
        public void DoesNotHandleEventWhenTextIsEmpty() {
            TextBox box;
            KeyEventArgs args;


            box = new TextBox { Text = "" };

            ClearOnEscape.SetEnabled(box, true);
            args = SendKeyDown(box, Key.Return);

            Assert.False(args.Handled);
            Assert.Equal("", box.Text);
        }



        [WpfFact]
        public void DoesNotHandleEventWhenPropertyIsDisabled() {
            TextBox box;
            KeyEventArgs args;


            box = new TextBox { Text = "test" };

            // Enable the property first.
            ClearOnEscape.SetEnabled(box, true);

            // Now disable the property and send the key press.
            ClearOnEscape.SetEnabled(box, false);
            args = SendKeyDown(box, Key.Return);
            Assert.False(args.Handled);
            Assert.Equal("test", box.Text);
        }


        private static KeyEventArgs SendKeyDown(TextBox box, Key key) {
            KeyEventArgs args;


            args = new KeyEventArgs(Keyboard.PrimaryDevice, Mock.Of<PresentationSource>(), 0, key) {
                RoutedEvent = UIElement.KeyDownEvent
            };

            box.RaiseEvent(args);

            return args;
        }

    }

}
