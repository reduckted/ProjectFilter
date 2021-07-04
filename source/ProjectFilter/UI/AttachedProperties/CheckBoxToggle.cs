using System;
using System.Windows;
using System.Windows.Input;


namespace ProjectFilter.UI.AttachedProperties {

    public static class CheckBoxToggle {

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(CheckBoxToggle),
            new PropertyMetadata(false, OnEnabledChanged)
        );


        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached(
            "IsChecked",
            typeof(bool?),
            typeof(CheckBoxToggle),
            new FrameworkPropertyMetadata((bool?)false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );


        private static readonly DependencyProperty IsSpaceKeyDownProperty = DependencyProperty.RegisterAttached(
            "IsSpaceKeyDown",
            typeof(bool),
            typeof(CheckBoxToggle),
            new FrameworkPropertyMetadata(false)
        );


        public static bool GetEnabled(DependencyObject obj) {
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            return (bool)obj.GetValue(EnabledProperty);
        }


        public static void SetEnabled(DependencyObject obj, bool value) {
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            obj.SetValue(EnabledProperty, value);
        }


        public static bool? GetIsChecked(DependencyObject obj) {
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            return (bool?)obj.GetValue(IsCheckedProperty);
        }


        public static void SetIsChecked(DependencyObject obj, bool? value) {
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            obj.SetValue(IsCheckedProperty, value);
        }


        private static bool GetIsSpaceKeyDown(DependencyObject obj) {
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            return (bool)obj.GetValue(IsSpaceKeyDownProperty);
        }


        public static void SetIsSpaceKeyDown(DependencyObject obj, bool value) {
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            obj.SetValue(IsSpaceKeyDownProperty, value);
        }


        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            UIElement? element;


            element = d as UIElement;

            if (element is not null) {
                if ((bool)e.OldValue) {
                    element.KeyDown -= OnElementKeyDown;
                    element.KeyUp -= OnElementKeyUp;
                    element.LostKeyboardFocus -= OnElementLostKeyboardFocus;
                }

                if ((bool)e.NewValue) {
                    element.KeyDown += OnElementKeyDown;
                    element.KeyUp += OnElementKeyUp;
                    element.LostKeyboardFocus += OnElementLostKeyboardFocus;
                }
            }
        }


        private static void OnElementKeyDown(object sender, KeyEventArgs e) {
            UIElement element;


            element = (UIElement)sender;

            // This is similar behavior to `ButtonBase.OnKeyDown()`.

            if (e.Key == Key.Space) {
                // Alt+Space should open the system menu, so don't handle it.
                if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                    if (e.OriginalSource == sender) {
                        SetIsSpaceKeyDown(element, true);
                        e.Handled = true;
                    }
                }

            } else {
                SetIsSpaceKeyDown(element, false);
            }
        }


        private static void OnElementKeyUp(object sender, KeyEventArgs e) {
            UIElement element;


            element = (UIElement)sender;

            // This is similar behavior to `ButtonBase.OnKeyDown()`.

            if (e.Key == Key.Space && GetIsSpaceKeyDown(element)) {
                // Alt+Space should open the system menu, so don't handle it.
                if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                    SetIsSpaceKeyDown(element, false);
                    SetIsChecked(element, !(GetIsChecked(element) ?? false));

                    e.Handled = true;
                }
            }
        }


        private static void OnElementLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            // This is similar behavior to `ButtonBase.OnKeyDown()`.
            if (e.OriginalSource == sender) {
                SetIsSpaceKeyDown((DependencyObject)sender, false);
            }
        }

    }

}
