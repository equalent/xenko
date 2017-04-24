// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// An object that tracks the changes in the content of <see cref="IGraphNode"/> referenced by a given root node.
    /// A <see cref="GraphNodeChangeListener"/> will raise events on changes on any node that is either a child, or the
    /// target of a reference from the root node, recursively.
    /// </summary>
    public class GraphNodeChangeListener : INotifyNodeValueChange, INotifyNodeItemChange, IDisposable
    {
        private readonly IGraphNode rootNode;
        private readonly Func<IMemberNode, bool> shouldRegisterMemberTarget;
        private readonly Func<IGraphNode, Index, bool> shouldRegisterItemTarget;
        protected readonly HashSet<IGraphNode> RegisteredNodes = new HashSet<IGraphNode>();

        public GraphNodeChangeListener(IGraphNode rootNode, Func<IMemberNode, bool> shouldRegisterMemberTarget = null, Func<IGraphNode, Index, bool> shouldRegisterItemTarget = null)
        {
            this.rootNode = rootNode;
            this.shouldRegisterMemberTarget = shouldRegisterMemberTarget;
            this.shouldRegisterItemTarget = shouldRegisterItemTarget;
            RegisterAllNodes();
        }

        /// <summary>
        /// Raised before one of the node referenced by the related root node changes.
        /// </summary>
        public event EventHandler<MemberNodeChangeEventArgs> ValueChanging;

        /// <summary>
        /// Raised after one of the node referenced by the related root node has changed.
        /// </summary>
        public event EventHandler<MemberNodeChangeEventArgs> ValueChanged;

        public event EventHandler<ItemChangeEventArgs> ItemChanging;

        public event EventHandler<ItemChangeEventArgs> ItemChanged;

        /// <inheritdoc/>
        public void Dispose()
        {
            var visitor = new GraphVisitorBase();
            visitor.Visiting += (node, path) => UnregisterNode(node);
            visitor.Visit(rootNode);
        }

        protected virtual bool RegisterNode(IGraphNode node)
        {
            // A node can be registered multiple times when it is referenced via multiple paths
            if (RegisteredNodes.Add(node))
            {
                ((IGraphNodeInternal)node).PrepareChange += ContentPrepareChange;
                ((IGraphNodeInternal)node).FinalizeChange += ContentFinalizeChange;
                var memberNode = node as IMemberNode;
                if (memberNode != null)
                {
                    memberNode.ValueChanging += OnValueChanging;
                    memberNode.ValueChanged += OnValueChanged;
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null)
                {
                    objectNode.ItemChanging += OnItemChanging;
                    objectNode.ItemChanged += OnItemChanged;
                }
                return true;
            }

            return false;
        }

        protected virtual bool UnregisterNode(IGraphNode node)
        {
            if (RegisteredNodes.Remove(node))
            {
                ((IGraphNodeInternal)node).PrepareChange -= ContentPrepareChange;
                ((IGraphNodeInternal)node).FinalizeChange -= ContentFinalizeChange;
                var memberNode = node as IMemberNode;
                if (memberNode != null)
                {
                    memberNode.ValueChanging -= OnValueChanging;
                    memberNode.ValueChanged -= OnValueChanged;
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null)
                {
                    objectNode.ItemChanging -= OnItemChanging;
                    objectNode.ItemChanged -= OnItemChanged;
                }
                return true;
            }
            return false;
        }

        private void RegisterAllNodes()
        {
            var visitor = new GraphVisitorBase();
            visitor.Visiting += (node, path) => RegisterNode(node);
            visitor.ShouldVisitMemberTargetNode =  shouldRegisterMemberTarget;
            visitor.ShouldVisitTargetItemNode = shouldRegisterItemTarget;
            visitor.Visit(rootNode);
        }

        private void ContentPrepareChange(object sender, INodeChangeEventArgs e)
        {
            var node = e.Node;
            var visitor = new GraphVisitorBase();
            visitor.Visiting += (node1, path) => UnregisterNode(node1);
            visitor.ShouldVisitMemberTargetNode = shouldRegisterMemberTarget;
            visitor.ShouldVisitTargetItemNode = shouldRegisterItemTarget;
            switch (e.ChangeType)
            {
                case ContentChangeType.ValueChange:
                case ContentChangeType.CollectionUpdate:
                    // The changed node itself is still valid, we don't want to unregister it
                    visitor.SkipRootNode = true;
                    visitor.Visit(node);
                    // TODO: In case of CollectionUpdate we could probably visit only the target node of the corresponding index
                    break;
                case ContentChangeType.CollectionRemove:
                    if (node.IsReference && e.OldValue != null)
                    {
                        var removedNode = (node as IObjectNode)?.ItemReferences[((ItemChangeEventArgs)e).Index].TargetNode;
                        if (removedNode != null)
                        {
                            // TODO: review this
                            visitor.Visit(removedNode, node as MemberNode);
                        }
                    }
                    break;
            }
        }

        private void ContentFinalizeChange(object sender, INodeChangeEventArgs e)
        {
            var visitor = new GraphVisitorBase();
            visitor.Visiting += (node, path) => RegisterNode(node);
            visitor.ShouldVisitMemberTargetNode = shouldRegisterMemberTarget;
            visitor.ShouldVisitTargetItemNode = shouldRegisterItemTarget;
            switch (e.ChangeType)
            {
                case ContentChangeType.ValueChange:
                case ContentChangeType.CollectionUpdate:
                    // The changed node itself is still valid, we don't want to re-register it
                    visitor.SkipRootNode = true;
                    visitor.Visit(e.Node);
                    // TODO: In case of CollectionUpdate we could probably visit only the target node of the corresponding index
                    break;
                case ContentChangeType.CollectionAdd:
                    if (e.Node.IsReference && e.NewValue != null)
                    {
                        IGraphNode addedNode;
                        Index index;
                        var arg = (ItemChangeEventArgs)e;
                        if (!arg.Index.IsEmpty)
                        {
                            index = arg.Index;
                            addedNode = (e.Node as IObjectNode)?.ItemReferences[arg.Index].TargetNode;
                        }
                        else
                        {
                            // TODO: review this
                            var reference = (e.Node as IObjectNode)?.ItemReferences.First(x => x.TargetNode.Retrieve() == e.NewValue);
                            index = reference.Index;
                            addedNode = reference.TargetNode;
                        }

                        if (addedNode != null && (shouldRegisterItemTarget?.Invoke(e.Node, index) ?? true))
                        {
                            var path = new GraphNodePath(e.Node);
                            path.PushIndex(index);
                            visitor.Visit(addedNode, e.Node as MemberNode, path);
                        }
                    }
                    break;
            }
        }

        private void OnValueChanging(object sender, MemberNodeChangeEventArgs e)
        {
            ValueChanging?.Invoke(sender, e);
        }

        private void OnValueChanged(object sender, MemberNodeChangeEventArgs e)
        {
            ValueChanged?.Invoke(sender, e);
        }

        private void OnItemChanging(object sender, ItemChangeEventArgs e)
        {
            ItemChanging?.Invoke(sender, e);
        }

        private void OnItemChanged(object sender, ItemChangeEventArgs e)
        {
            ItemChanged?.Invoke(sender, e);
        }
    }
}
