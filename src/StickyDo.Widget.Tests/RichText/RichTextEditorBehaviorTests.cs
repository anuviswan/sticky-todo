using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Widget.Controls.Behaviors;

namespace StickyDo.Widget.Tests.RichText;

[TestClass]
public partial class RichTextEditorBehaviorTests
{
    private partial class TestVm : ObservableObject
    {
        [ObservableProperty]
        private string plainText = string.Empty;
    }

    [TestMethod]
    public void TypingIntoBoundRichTextBox_UpdatesSourceProperty()
    {
        string? result = null;
        var thread = new System.Threading.Thread(() =>
        {
            var vm = new TestVm();
            var richTextBox = new RichTextBox();
            BindingOperations.SetBinding(richTextBox, RichTextEditorBehavior.PlainTextProperty,
                new Binding(nameof(TestVm.PlainText)) { Source = vm, Mode = BindingMode.TwoWay });

            // A RichTextBox not attached to a visual tree never becomes IsLoaded, so this exercises
            // the "wait for Loaded" path; raising it manually stands in for what a real Window does.
            richTextBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            // Insert text the same way real keystrokes do (via a TextPointer in the live document),
            // not by touching PlainText/the VM directly.
            richTextBox.CaretPosition.DocumentEnd.InsertTextInRun("H");

            result = vm.PlainText;
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.AreEqual("H", result);
    }
}
