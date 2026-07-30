using System.Windows.Forms;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of <see cref="ITrayIconService"/> backed by
/// <see cref="System.Windows.Forms.NotifyIcon"/>, since WPF has no native tray icon API.
/// </summary>
public class TrayIconService : ITrayIconService
{
    private NotifyIcon? _notifyIcon;

    public void Initialize(Action onOpenRequested, Action onExitRequested)
    {
        ArgumentNullException.ThrowIfNull(onOpenRequested);
        ArgumentNullException.ThrowIfNull(onExitRequested);

        if (_notifyIcon != null)
            return;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open StickyDo", null, (s, e) => onOpenRequested());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) => onExitRequested());

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "StickyDo",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => onOpenRequested();
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        var resourceInfo = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/app.ico"));

        if (resourceInfo == null)
            return System.Drawing.SystemIcons.Application;

        using var stream = resourceInfo.Stream;
        return new System.Drawing.Icon(stream);
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        GC.SuppressFinalize(this);
    }
}
