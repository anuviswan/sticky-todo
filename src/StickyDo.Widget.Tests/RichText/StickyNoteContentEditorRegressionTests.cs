using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;

namespace StickyDo.Widget.Tests.RichText;

/// <summary>
/// Regression test for a bug where typing into a brand-new (empty) note's content box never
/// reached the ViewModel: RichTextEditorBehavior originally scheduled its one-time initialization
/// from PlainText/Formatting's PropertyChangedCallback, but WPF only invokes that callback when a
/// property's value actually differs from its default - and a new note's Content is "", identical
/// to PlainTextProperty's own default, so the callback (and therefore initialization) never ran.
/// Fixed by moving initialization to a CoerceValueCallback, which runs on every SetValue
/// regardless of the resulting value. Uses genuinely compiled XAML (see
/// StickyNoteContentEditorProbe.xaml), matching how StickyNoteWindow.xaml itself is built -
/// XamlReader.Parse-based dynamic XAML was tried first and has its own unrelated Loaded-firing
/// quirk that produced misleading results during investigation.
/// </summary>
[TestClass]
public partial class StickyNoteContentEditorRegressionTests
{
    private partial class TestVm : ObservableObject
    {
        [ObservableProperty]
        private NoteType type = NoteType.Note;

        [ObservableProperty]
        private string content = string.Empty;

        [ObservableProperty]
        private RichTextFormatting? contentFormatting;
    }

    [TestMethod]
    public void TypingIntoBrandNewNote_UpdatesContentAndHidesPlaceholder()
    {
        string? finalContent = null;
        Visibility placeholderVisibility = Visibility.Visible;

        var thread = new System.Threading.Thread(() =>
        {
            var vm = new TestVm(); // Content/ContentFormatting stay at their defaults - the bug case.
            var probe = new StickyNoteContentEditorProbe();

            var window = new Window { Content = probe, DataContext = vm, Width = 300, Height = 300, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
            window.Show();
            Pump();

            probe.ContentRichTextBox.Focus();
            probe.ContentRichTextBox.CaretPosition.DocumentEnd.InsertTextInRun("Hello");
            Pump();

            finalContent = vm.Content;
            placeholderVisibility = probe.Placeholder.Visibility;

            window.Close();
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.AreEqual("Hello", finalContent);
        Assert.AreEqual(Visibility.Collapsed, placeholderVisibility);
    }

    /// <summary>
    /// A real keystroke is a discrete, dispatcher-processed Win32 message, so there's always a
    /// return-to-idle between one keystroke and whatever reads the bound VM next; this drains the
    /// queue enough for that TwoWay binding update to land, mirroring what always happens before a
    /// user could observe the result of typing.
    /// </summary>
    private static void Pump()
    {
        for (var i = 0; i < 5; i++)
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
    }
}
