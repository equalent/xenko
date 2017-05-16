﻿// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SiliconStudio.Core.Streaming;

namespace SiliconStudio.Xenko.Streaming
{
    /// <summary>
    /// Streamable resources content management service.
    /// </summary>
    public class ContentStreamingService : IDisposable
    {
        private readonly Dictionary<int, ContentStorage> containers = new Dictionary<int, ContentStorage>();
        
        // Configuration
        public TimeSpan UnusedDataChunksLifetime = TimeSpan.FromSeconds(3);

        internal Func<Task<IDisposable>> MountDatabase { get; set; }

        internal ContentStreamingService()
        {
            MountDatabase = () => Task.FromResult((IDisposable)null);
        }

        /// <summary>
        /// Gets the storage container.
        /// </summary>
        /// <param name="storageHeader">The storage header.</param>
        /// <returns>Content Storage container.</returns>
        public ContentStorage GetStorage(ref ContentStorageHeader storageHeader)
        {
            ContentStorage result;

            lock (containers)
            {
                int hash = storageHeader.DataUrl.GetHashCode();
                if (!containers.TryGetValue(hash, out result))
                {
                    result = new ContentStorage(this, ref storageHeader);
                    containers.Add(hash, result);
                }
            }

            Debug.Assert(result != null && result.Url == storageHeader.DataUrl);
            return result;
        }

        internal void Update()
        {
            lock (containers)
            {
                foreach (var e in containers)
                    e.Value.ReleaseUnusedChunks();
            }
        }

        public void Dispose()
        {
            lock (containers)
            {
                containers.Clear();
            }
        }
    }
}
