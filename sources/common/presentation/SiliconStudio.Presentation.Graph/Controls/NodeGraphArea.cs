﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using GraphX;
using GraphX.GraphSharp;
using QuickGraph;
using SiliconStudio.Presentation.Graph.ViewModel;
using System.Windows;

namespace SiliconStudio.Presentation.Graph.Controls
{    
    /// <summary>
    /// 
    /// </summary>
    public class NodeGraphArea : GraphArea<NodeVertex, NodeEdge, BidirectionalGraph<NodeVertex, NodeEdge>> 
    {
        public virtual event LinkSelectedEventHandler LinkSelected;

        internal virtual void OnLinkSelected(FrameworkElement link)
        {
            if (LinkSelected != null)
            {
                LinkSelected(this, new LinkSelectedEventArgs(link));
            }
        }
    }
}