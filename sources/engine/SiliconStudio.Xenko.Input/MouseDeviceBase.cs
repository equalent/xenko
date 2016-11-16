﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Xenko.Input
{
    /// <summary>
    /// Base class for mouse devices, implements some common functionality of <see cref="IMouseDevice"/>, inherits from <see cref="PointerDeviceBase"/>
    /// </summary>
    public abstract class MouseDeviceBase : PointerDeviceBase, IMouseDevice
    {
        public readonly HashSet<MouseButton> DownButtons = new HashSet<MouseButton>();
        protected readonly List<InputEvent> EventQueue = new List<InputEvent>();
        protected Vector2 position;

        public abstract bool IsPositionLocked { get; }
        
        public Vector2 Position => position;

        public override PointerType Type => PointerType.Mouse;
        
        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);

            if (PointerDatas.Count > 0)
            {
                // Mouse position == first pointer position
                position = PointerDatas[0].Position;
            }
            
            // Collect events from queue
            foreach (var evt in EventQueue)
            {
                inputEvents.Add(evt);
            }
            EventQueue.Clear();
        }
        
        public virtual bool IsButtonDown(MouseButton button)
        {
            return DownButtons.Contains(button);
        }
        
        public abstract void SetPosition(Vector2 normalizedPosition);
        
        public abstract void LockPosition(bool forceCenter = false);
        
        public abstract void UnlockPosition();

        /// <summary>
        /// Special move that generates pointer events with just delta
        /// </summary>
        /// <param name="delta">The movement delta</param>
        protected void HandleMouseDelta(Vector2 delta)
        {
            if (delta == Vector2.Zero)
                return;

            // Normalize delta
            delta *= InverseSurfaceSize;

            var data = GetPointerData(0);

            // Update pointer position + delta
            // Update delta
            data.Delta = delta;
            
            data.PointerClock.Restart();

            var pointerEvent = InputEventPool<PointerEvent>.GetOrCreate(this);
            pointerEvent.Position = data.Position;
            pointerEvent.DeltaPosition = data.Delta;
            pointerEvent.DeltaTime = data.PointerClock.Elapsed;
            pointerEvent.IsDown = data.Down;
            pointerEvent.PointerId = 0;
            pointerEvent.PointerType = Type;
            pointerEvent.EventType = PointerEventType.Moved;

            EventQueue.Add(pointerEvent);
        }

        public void HandleButtonDown(MouseButton button)
        {
            // Prevent duplicate events
            if (DownButtons.Contains(button))
                return;

            DownButtons.Add(button);

            var buttonEvent = InputEventPool<MouseButtonEvent>.GetOrCreate(this);
            buttonEvent.Button = button;
            buttonEvent.State = ButtonState.Down;
            EventQueue.Add(buttonEvent);

            // Simulate tap on primary mouse button
            if (button == MouseButton.Left)
                HandlePointerDown();
        }

        public void HandleButtonUp(MouseButton button)
        {
            // Prevent duplicate events
            if (!DownButtons.Contains(button))
                return;

            DownButtons.Remove(button);

            var buttonEvent = InputEventPool<MouseButtonEvent>.GetOrCreate(this);
            buttonEvent.Button = button;
            buttonEvent.State = ButtonState.Up;
            EventQueue.Add(buttonEvent);

            // Simulate tap on primary mouse button
            if (button == MouseButton.Left)
                HandlePointerUp();
        }

        public void HandleMouseWheel(float wheelDelta)
        {
            var wheelEvent = InputEventPool<MouseWheelEvent>.GetOrCreate(this);
            wheelEvent.WheelDelta = wheelDelta;
            EventQueue.Add(wheelEvent);
        }

        protected struct MouseInputEvent
        {
            public MouseButton Button;
            public MouseInputEventType Type;
            public float WheelDelta;
        }

        protected enum MouseInputEventType
        {
            Up,
            Down,
            Scroll,
        }
    }
}