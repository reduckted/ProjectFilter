using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;


namespace ProjectFilter.UI.Converters;


public class TreeViewItemIndentConverter : IValueConverter {

    public double Length { get; set; }


    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        TreeViewItem? item;
        int depth;


        item = value as TreeViewItem;
        depth = 0;

        while (item is not null) {
            item = GetParentItem(item);

            if (item is not null) {
                depth += 1;
            }
        }

        return new Thickness(Length * depth, 0, 0, 0);
    }


    private static TreeViewItem? GetParentItem(TreeViewItem item) {
        DependencyObject obj;


        obj = item;

        while (true) {
            obj = VisualTreeHelper.GetParent(obj);

            // If we reached the top of the visual tree of we reached 
            // the `TreeView`, then there is no parent `TreeViewItem`.
            if (obj is null || obj is TreeView) {
                return null;
            }

            // If we reached a `TreeViewItem` then that's the parent item.
            if (obj is TreeViewItem parent) {
                return parent;
            }
        }
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return DependencyProperty.UnsetValue;
    }

}
