using Microsoft.VisualStudio;
using System;
using System.Windows;


namespace ProjectFilter.UI.AttachedProperties {

    public static class FocusWhenVisible {

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(FocusWhenVisible),
            new PropertyMetadata(false, OnEnabledChanged)
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


        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            UIElement? target;


            target = d as UIElement;

            if (target is not null) {
                if ((bool)e.OldValue) {
                    target.IsVisibleChanged -= OnIsVisibleChanged;
                }

                if ((bool)e.NewValue) {
                    target.IsVisibleChanged += OnIsVisibleChanged;
                }
            }
        }


        private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            UIElement? target;


            target = sender as UIElement;

            if (target is not null && target.IsVisible) {
                try {
                    target.Focus();
                } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

    }

}
