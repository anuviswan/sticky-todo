using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Services;
using StickyDo.Widget.Messages;

namespace StickyDo.Widget.Services;

/// <summary>
/// Decorates <see cref="PersistenceService"/>, forwarding its
/// <see cref="IPersistenceService.NoteSaveStateChanged"/> event onto the UI-facing
/// <see cref="IMessenger"/> as a <see cref="NoteSaveStateChangedMessage"/>, so note windows
/// can subscribe (weakly, via <see cref="IMessenger.Register{TMessage}(object, MessageHandler{object, TMessage})"/>)
/// without the Domain project taking a dependency on CommunityToolkit.Mvvm.
/// </summary>
public class MessengerBridgedPersistenceService : IPersistenceService
{
    private readonly PersistenceService _inner;

    public MessengerBridgedPersistenceService(PersistenceService inner, IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(messenger);
        _inner = inner;

        _inner.NoteSaveStateChanged += (_, e) =>
            messenger.Send(new NoteSaveStateChangedMessage(e.NoteId, e.State));
    }

    public event EventHandler<NoteSaveStateChangedEventArgs>? NoteSaveStateChanged
    {
        add => _inner.NoteSaveStateChanged += value;
        remove => _inner.NoteSaveStateChanged -= value;
    }

    public void StartAutoSave() => _inner.StartAutoSave();

    public Task StopAutoSaveAsync() => _inner.StopAutoSaveAsync();

    public Task SaveAllDirtyNotesAsync() => _inner.SaveAllDirtyNotesAsync();

    public bool HasPendingChanges => _inner.HasPendingChanges;
}
