using System;
using System.Windows;


namespace ProjectFilter.UI.AttachedProperties;


public static class CloseDialog {

    public static readonly DependencyProperty TriggerProperty = DependencyProperty.RegisterAttached(
         "Trigger",
         typeof(object),
         typeof(CloseDialog),
         new PropertyMetadata(null, OnTriggerChanged)
     );


    public static bool GetTrigger(DependencyObject obj) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        return (bool)obj.GetValue(TriggerProperty);
    }


    public static void SetTrigger(DependencyObject obj, object value) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        obj.SetValue(TriggerProperty, value);
    }


    private static void OnTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Window? dialog;


        dialog = d as Window;

        if (dialog is not null) {
            // Set the dialog result to true when the trigger value 
            // changes. The property is initially null, so as soon as 
            // it's set to a non-null value, the dialog should close.
            dialog.DialogResult = true;
        }
    }

}
