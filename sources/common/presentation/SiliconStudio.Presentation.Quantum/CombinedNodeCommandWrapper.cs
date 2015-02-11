﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.ActionStack;
using SiliconStudio.Presentation.ViewModel;
using SiliconStudio.Quantum;

namespace SiliconStudio.Presentation.Quantum
{
    public class CombinedNodeCommandWrapper : NodeCommandWrapperBase
    {
        private readonly IReadOnlyCollection<ModelNodeCommandWrapper> commands;
        private readonly IViewModelServiceProvider serviceProvider;
        private readonly string name;
        private readonly ObservableViewModelService service;
        private readonly ObservableViewModelIdentifier identifier;

        public CombinedNodeCommandWrapper(IViewModelServiceProvider serviceProvider, string name, string observableNodePath, ObservableViewModelIdentifier identifier, IReadOnlyCollection<ModelNodeCommandWrapper> commands)
            : base(serviceProvider, null)
        {
            if (commands == null) throw new ArgumentNullException("commands");
            if (commands.Count == 0) throw new ArgumentException(@"The collection of commands to combine is empty", "commands");
            if (commands.Any(x => !ReferenceEquals(x.NodeCommand, commands.First().NodeCommand))) throw new ArgumentException(@"The collection of commands to combine cannot contain different node commands", "commands");
            service = serviceProvider.Get<ObservableViewModelService>();
            this.commands = commands;
            this.name = name;
            this.identifier = identifier;
            this.serviceProvider = serviceProvider;
            ObservableNodePath = observableNodePath;
        }

        public override string Name { get { return name; } }

        public override CombineMode CombineMode { get { return CombineMode.DoNotCombine; } }

        private ITransactionalActionStack ActionStack { get { return serviceProvider.Get<ITransactionalActionStack>(); } }
        
        public override void Execute(object parameter)
        {
            ActionStack.BeginTransaction();
            Redo(parameter, true);
            var displayName = "Executing " + Name;

            var observableViewModel = service.ViewModelProvider(identifier);
            if (observableViewModel != null && !commands.Any(x => observableViewModel.MatchCombinedRootNode(x.GetCommandRootNode())))
                observableViewModel = null;

            var node = observableViewModel != null ? observableViewModel.ResolveObservableNode(ObservableNodePath) as CombinedObservableNode : null;
            // TODO: this need to be verified but I suppose node is never null
            ActionStack.EndTransaction(displayName, x => new CombinedValueChangedActionItem(displayName, service, node != null ? node.Path : null, identifier, x));
        }

        protected override UndoToken Redo(object parameter, bool creatingActionItem)
        {
            var undoTokens = new Dictionary<ModelNodeCommandWrapper, UndoToken>();
            bool canUndo = false;

            commands.First().NodeCommand.StartCombinedInvoke();

            foreach (var command in commands)
            {
                var undoToken = command.ExecuteCommand(parameter, creatingActionItem);
                undoTokens.Add(command, undoToken);
                canUndo = canUndo || undoToken.CanUndo;
            }

            commands.First().NodeCommand.EndCombinedInvoke();

            Refresh();
            return new UndoToken(canUndo, undoTokens);
        }

        protected override void Undo(object parameter, UndoToken token)
        {
            var undoTokens = (Dictionary<ModelNodeCommandWrapper, UndoToken>)token.TokenValue;
            foreach (var command in commands)
            {
                command.UndoCommand(parameter, undoTokens[command]);
            }
            Refresh();
        }

        private void Refresh()
        {
            var observableViewModel = service.ViewModelProvider(identifier);
            if (observableViewModel != null && !commands.Any(x => observableViewModel.MatchCombinedRootNode(x.GetCommandRootNode())))
                observableViewModel = null;

            var observableNode = observableViewModel != null ? observableViewModel.ResolveObservableNode(ObservableNodePath) as CombinedObservableNode : null;

            // Recreate observable nodes to apply changes
            if (observableNode != null)
            {
                observableNode.Refresh();
                observableNode.Owner.NotifyNodeChanged(observableNode.Path);
            }
        }
    }
}
