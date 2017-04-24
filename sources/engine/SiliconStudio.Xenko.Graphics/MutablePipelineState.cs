// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Graphics
{
    public class MutablePipelineState
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly Dictionary<PipelineStateDescriptionWithHash, PipelineState> cache;
        public PipelineStateDescription State;

        /// <summary>
        /// Current compiled state.
        /// </summary>
        public PipelineState CurrentState;

        public MutablePipelineState(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            cache = graphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, typeof(MutablePipelineStateCache), device => new MutablePipelineStateCache()).Cache;

            State = new PipelineStateDescription();
            State.SetDefaults();
        }

        /// <summary>
        /// Determine and updates <see cref="CurrentState"/> from <see cref="State"/>.
        /// </summary>
        public void Update()
        {
            // Hash current state
            var hashedState = new PipelineStateDescriptionWithHash(State);

            // Find existing PipelineState object
            PipelineState pipelineState;

            // TODO GRAPHICS REFACTOR We could avoid lock by adding them to a ThreadLocal (or RenderContext) and merge at end of frame
            lock (cache)
            {
                if (!cache.TryGetValue(hashedState, out pipelineState))
                {
                    // Otherwise, instantiate it
                    // First, make an copy
                    hashedState = new PipelineStateDescriptionWithHash(State.Clone());
                    cache.Add(hashedState, pipelineState = PipelineState.New(graphicsDevice, ref State));
                }
            }

            CurrentState = pipelineState;
        }

        class MutablePipelineStateCache : IDisposable
        {
            public readonly Dictionary<PipelineStateDescriptionWithHash, PipelineState> Cache = new Dictionary<PipelineStateDescriptionWithHash, PipelineState>();

            public void Dispose()
            {
                foreach (var pipelineState in Cache)
                {
                    ((IReferencable)pipelineState.Value).Release();
                }

                Cache.Clear();
            }
        }
    }
}
