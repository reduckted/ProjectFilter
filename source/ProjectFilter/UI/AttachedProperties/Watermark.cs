using System;
using System.Windows;
using System.Windows.Controls;


namespace ProjectFilter.UI.AttachedProperties;


public static class Watermark {

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(Watermark),
        new PropertyMetadata(null, OnTextChanged)
    );


    private static readonly DependencyPropertyKey HidePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "Hide",
        typeof(bool),
        typeof(Watermark),
        new PropertyMetadata(false)
    );


    public static readonly DependencyProperty HideProperty = HidePropertyKey.DependencyProperty;


    public static string GetText(DependencyObject obj) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        return (string)obj.GetValue(TextProperty);
    }


    public static void SetText(DependencyObject obj, string value) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        obj.SetValue(TextProperty, value);
    }


    public static bool GetHide(DependencyObject obj) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        return (bool)obj.GetValue(HideProperty);
    }


    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TextBox? box;


        box = d as TextBox;

        if (box is not null) {
            if (e.OldValue is not null) {
                box.TextChanged -= OnTextBoxTextChanged;
                box.ClearValue(HidePropertyKey);
            }

            if (e.NewValue is not null) {
                box.TextChanged += OnTextBoxTextChanged;
                UpdateHideProperty(box);
            }
        }
    }


    private static void OnTextBoxTextChanged(object sender, TextChangedEventArgs e) {
        UpdateHideProperty((TextBox)sender);
    }


    private static void UpdateHideProperty(TextBox box) {
        box.SetValue(HidePropertyKey, !string.IsNullOrEmpty(box.Text));
    }

}
