using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ProjectFilter.UI.AttachedProperties {

    public class ClearOnEscape {

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
              "Enabled",
              typeof(bool),
              typeof(ClearOnEscape),
              new PropertyMetadata(false, OnEnabledChanged)
          );



        public static bool GetEnabled(DependencyObject obj) {
            return (bool)obj.GetValue(EnabledProperty);
        }


        public static void SetEnabled(DependencyObject obj, bool value) {
            obj.SetValue(EnabledProperty, value);
        }


        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            TextBox? box;


            box = d as TextBox;

            if (box != null) {
                if ((bool)e.OldValue) {
                    box.KeyDown -= OnTextBoxKeyDown;
                }

                if ((bool)e.NewValue) {
                    box.KeyDown += OnTextBoxKeyDown;
                }
            }
        }


        private static void OnTextBoxKeyDown(object sender, KeyEventArgs e) {
            if (e.OriginalSource == sender) {
                if (e.Key == Key.Escape) {
                    TextBox box;


                    box = (TextBox)sender;

                    if (!string.IsNullOrEmpty(box.Text)) {
                        box.Text = "";
                        e.Handled = true;
                    }
                }
            }
        }

    }

}
