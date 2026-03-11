using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    public abstract class BTNode<TBlackboard>
    {
        protected BTNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public abstract NodeState Tick(TBlackboard blackboard, float deltaTime);

        public virtual void Reset() { }
    }

    public abstract class CompositeNode<TBlackboard> : BTNode<TBlackboard>
    {
        protected CompositeNode(string name) : base(name)
        {
        }

        protected readonly List<BTNode<TBlackboard>> Children = new();

        public CompositeNode<TBlackboard> AddChild(BTNode<TBlackboard> child)
        {
            Children.Add(child);
            return this;
        }

        public override void Reset()
        {
            foreach (var child in Children)
            {
                child.Reset();
            }
        }
    }

    public sealed class ConditionNode<TBlackboard> : BTNode<TBlackboard>
    {
        private readonly Func<TBlackboard, bool> _predicate;

        public ConditionNode(string name, Func<TBlackboard, bool> predicate) : base(name)
        {
            _predicate = predicate;
        }

        public override NodeState Tick(TBlackboard blackboard, float deltaTime)
        {
            return _predicate(blackboard) ? NodeState.Success : NodeState.Failure;
        }
    }

    public sealed class ParallelAllNode<TBlackboard> : CompositeNode<TBlackboard>
    {
        private readonly List<NodeState> _childStates = new();

        public ParallelAllNode(string name) : base(name)
        {
        }

        public override NodeState Tick(TBlackboard blackboard, float deltaTime)
        {
            if (_childStates.Count != Children.Count)
            {
                _childStates.Clear();
                for (var i = 0; i < Children.Count; i++)
                {
                    _childStates.Add(NodeState.Running);
                }
            }

            var allSuccess = true;
            var hasFailure = false;

            for (var i = 0; i < Children.Count; i++)
            {
                if (_childStates[i] == NodeState.Success)
                {
                    continue;
                }

                if (_childStates[i] == NodeState.Failure)
                {
                    hasFailure = true;
                    continue;
                }

                _childStates[i] = Children[i].Tick(blackboard, deltaTime);

                if (_childStates[i] == NodeState.Failure)
                {
                    hasFailure = true;
                }

                if (_childStates[i] != NodeState.Success)
                {
                    allSuccess = false;
                }
            }

            if (hasFailure)
            {
                return NodeState.Failure;
            }

            if (allSuccess)
            {
                Reset();
                return NodeState.Success;
            }

            return NodeState.Running;
        }

        public override void Reset()
        {
            _childStates.Clear();
            base.Reset();
        }
    }

    public sealed class SelectorNode<TBlackboard> : CompositeNode<TBlackboard>
    {
        private int _currentIndex;

        public SelectorNode(string name) : base(name)
        {
        }

        public override NodeState Tick(TBlackboard blackboard, float deltaTime)
        {
            while (_currentIndex < Children.Count)
            {
                var state = Children[_currentIndex].Tick(blackboard, deltaTime);

                if (state == NodeState.Running)
                {
                    return NodeState.Running;
                }

                if (state == NodeState.Success)
                {
                    _currentIndex = 0;
                    return NodeState.Success;
                }

                _currentIndex++;
            }

            _currentIndex = 0;
            return NodeState.Failure;
        }

        public override void Reset()
        {
            _currentIndex = 0;
            base.Reset();
        }
    }

    public sealed class SequenceNode<TBlackboard> : CompositeNode<TBlackboard>
    {
        private int _currentIndex;

        public SequenceNode(string name) : base(name)
        {
        }

        public override NodeState Tick(TBlackboard blackboard, float deltaTime)
        {
            while (_currentIndex < Children.Count)
            {
                var state = Children[_currentIndex].Tick(blackboard, deltaTime);

                if (state == NodeState.Running)
                {
                    return NodeState.Running;
                }

                if (state == NodeState.Failure)
                {
                    _currentIndex = 0;
                    return NodeState.Failure;
                }

                _currentIndex++;
            }

            _currentIndex = 0;
            return NodeState.Success;
        }

        public override void Reset()
        {
            _currentIndex = 0;
            base.Reset();
        }
    }

    public sealed class WaitNode<TBlackboard> : BTNode<TBlackboard>
    {
        private readonly float _duration;
        private float _elapsed;

        public WaitNode(string name, float duration) : base(name)
        {
            _duration = duration;
        }

        public override NodeState Tick(TBlackboard blackboard, float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed < _duration)
            {
                return NodeState.Running;
            }

            _elapsed = 0f;
            return NodeState.Success;
        }

        public override void Reset()
        {
            _elapsed = 0f;
        }
    }

}

