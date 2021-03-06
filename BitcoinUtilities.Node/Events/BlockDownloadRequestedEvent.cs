﻿using BitcoinUtilities.Node.Modules.Blocks;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Events
{
    /// <summary>
    /// The <see cref="BlockStorageService"/> uses this event to inform the <see cref="BlockDownloadService"/>
    /// that the list of blocks that should be downloaded is changed.
    /// </summary>
    public sealed class BlockDownloadRequestedEvent : IEvent
    {
    }
}