using Microsoft.VisualStudio;
using ProjectFilter.UI.Utilities;
using System;
using System.Diagnostics;
using System.Windows;


namespace ProjectFilter.UI.AttachedProperties;


public static class FocusWhenRequested {

    public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
        "Source",
        typeof(FocusSource),
        typeof(FocusWhenRequested),
        new PropertyMetadata(null, OnSourceChanged)
    );


    private static readonly DependencyProperty EventHandlerProperty = DependencyProperty.RegisterAttached(
       "EventHandler",
       typeof(EventHandler),
       typeof(FocusWhenRequested),
       new PropertyMetadata(null)
   );


    public static FocusSource GetSource(DependencyObject obj) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        return (FocusSource)obj.GetValue(SourceProperty);
    }


    public static void SetSource(DependencyObject obj, FocusSource value) {
        if (obj is null) {
            throw new ArgumentNullException(nameof(obj));
        }

        obj.SetValue(SourceProperty, value);
    }


    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        UIElement? target;


        target = d as UIElement;

        if (target is not null) {
            EventHandler? handler;


            if (e.OldValue is FocusSource oldSource) {
                // Remove the event handler from the old source.
                // The event handler is saved against the target object.
                handler = target.GetValue(EventHandlerProperty) as EventHandler;
                target.SetValue(EventHandlerProperty, null);

                if (handler is not null) {
                    oldSource.FocusRequested -= handler;
                }
            }

            if (e.NewValue is FocusSource newSource) {
                handler = (_, _) => OnFocusRequested(target);
                newSource.FocusRequested += handler;

                // Save the event handler against the target so
                // that we can remove it of the source changes.
                target.SetValue(EventHandlerProperty, handler);
            }
        }
    }


    private static void OnFocusRequested(UIElement target) {
        try {
            target.Focus();
        } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
            Debug.WriteLine(ex);
        }
    }

}
