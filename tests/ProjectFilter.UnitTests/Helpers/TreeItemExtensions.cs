using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.Helpers;


internal static class TreeItemExtensions {

    public static IEnumerable<T> DescendantsAndSelf<T>(this T item) where T : TreeItem {
        return new[] { item }.Concat(item.Descendants());
    }


    public static IEnumerable<T> Descendants<T>(this T item) where T : TreeItem {
        return EnumerateTree(item);
    }


    private static IEnumerable<T> EnumerateTree<T>(T node) where T : TreeItem {
        foreach (var child in node.Children.Cast<T>()) {
            yield return child;

            foreach (var descendant in EnumerateTree(child)) {
                yield return descendant;
            }
        }
    }

}
