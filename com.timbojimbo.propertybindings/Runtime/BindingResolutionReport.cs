using System;
using System.Collections.Generic;

namespace TimboJimbo.PropertyBindings
{
    public sealed class BindingCandidateReport
    {
        public int Priority { get; }
        public Type BindingType { get; }
        public bool Matched { get; }
        public Type ConstructedBindingType { get; }
        public Exception Failure { get; }

        internal BindingCandidateReport(
            int priority,
            Type bindingType,
            bool matched,
            Type constructedBindingType,
            Exception failure)
        {
            Priority = priority;
            BindingType = bindingType;
            Matched = matched;
            ConstructedBindingType = constructedBindingType;
            Failure = failure;
        }
    }

    public sealed class BindingResolutionReport
    {
        public BindableProperty Property { get; }
        public bool IsLiveInstance { get; }
        public Type SelectedBindingType { get; }
        public IReadOnlyList<BindingCandidateReport> Candidates { get; }
        public bool Success => SelectedBindingType != null;

        internal BindingResolutionReport(
            BindableProperty property,
            bool isLiveInstance,
            Type selectedBindingType,
            IReadOnlyList<BindingCandidateReport> candidates)
        {
            Property = property;
            IsLiveInstance = isLiveInstance;
            SelectedBindingType = selectedBindingType;
            Candidates = candidates;
        }
    }
}