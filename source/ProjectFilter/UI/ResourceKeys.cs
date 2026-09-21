using Microsoft.VisualStudio.Shell;
using System;

namespace ProjectFilter.UI;

internal static class ResourceKeys {

    public static readonly Guid Category = new("73708ded-2d56-4aad-b8eb-73b20d3f4bff");


    public static readonly ThemeResourceKey AccentFillDefaultBrushKey = new(
        Category,
        "AccentFillDefault",
        ThemeResourceKeyType.BackgroundBrush
    );


    public static ThemeResourceKey SubtleFillSecondaryBrushKey = new(
        Category,
        "SubtleFillSecondary",
        ThemeResourceKeyType.BackgroundBrush
    );


    public static ThemeResourceKey ControlStrokeSecondaryBrushKey = new(
        Category,
        "ControlStrokeSecondary",
        ThemeResourceKeyType.BackgroundBrush
    );

}
