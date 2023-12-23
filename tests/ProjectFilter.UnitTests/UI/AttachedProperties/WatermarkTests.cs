using System;
using System.Windows.Controls;
using Xunit;


namespace ProjectFilter.UI.AttachedProperties;


public class WatermarkTests {

    [WpfFact]
    public void SetsHideToTrueWhenTheTextBoxContainsText() {
        TextBox box;


        box = new TextBox();
        Watermark.SetText(box, "foo");

        Assert.False(Watermark.GetHide(box));

        box.Text = "bar";

        Assert.True(Watermark.GetHide(box));
    }


    [WpfFact]
    public void SetsHideToTrueWhenTheTextBoxContainsWhitespace() {
        TextBox box;


        box = new TextBox();
        Watermark.SetText(box, "foo");

        Assert.False(Watermark.GetHide(box));

        box.Text = " ";

        Assert.True(Watermark.GetHide(box));
    }


    [WpfTheory]
    [InlineData("")]
    [InlineData(null)]
    public void SetsHideToFalseWhenTheTextBoxDoesNotContainText(string? watermark) {
        TextBox box;


        box = new TextBox();
        Watermark.SetText(box, "foo");

        box.Text = "bar";

        Assert.True(Watermark.GetHide(box));

        box.Text = watermark;

        Assert.False(Watermark.GetHide(box));
    }


    [WpfTheory]
    [InlineData("", false)]
    [InlineData("bar", true)]
    public void UpdatesHidePropertyWhenWatermarkTextIsSet(string initialText, bool hide) {
        TextBox box;


        box = new TextBox {
            Text = initialText
        };

        Watermark.SetText(box, "foo");

        Assert.Equal(hide, Watermark.GetHide(box));
    }


    [WpfFact]
    public void StopsUpdatingWhenWatermarkTextIsCleared() {
        TextBox box;


        box = new TextBox();
        Watermark.SetText(box, "foo");

        box.Text = "bar";
        Assert.True(Watermark.GetHide(box));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Watermark.SetText(box, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        Assert.False(Watermark.GetHide(box));

        box.Text = "";
        Assert.False(Watermark.GetHide(box));

        box.Text = "bar";
        Assert.False(Watermark.GetHide(box));
    }

}
