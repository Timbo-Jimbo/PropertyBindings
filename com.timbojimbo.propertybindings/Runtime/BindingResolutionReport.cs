using System;
using System.Collections.Generic;

namespace TimboJimbo.PropertyBindings
{
    public enum BindingMatchKind
    {
        None,
        Descriptor,
        Predicate
    }

    public sealed class BindingCandidateReport
    {
        public int Priority { get; }
        public Type BindingType { get; }
        public IReadOnlyList<string> DescriptorIds { get; }
        public BindingMatchKind MatchKind { get; }
        public bool Matched { get; }
        public Type ConstructedBindingType { get; }
        public Exception Failure { get; }

        internal BindingCandidateReport(
            int priority,
            Type bindingType,
            IReadOnlyList<string> descriptorIds,
            BindingMatchKind matchKind,
            bool matched,
            Type constructedBindingType,
            Exception failure)
        {
            Priority = priority;
            BindingType = bindingType;
            DescriptorIds = descriptorIds;
            MatchKind = matchKind;
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
        public IPropertyDescriptor ResolvedDescriptor { get; }
        public IReadOnlyList<BindingCandidateReport> Candidates { get; }
        public bool Success => SelectedBindingType != null;

        internal BindingResolutionReport(
            BindableProperty property,
            bool isLiveInstance,
            Type selectedBindingType,
            IPropertyDescriptor resolvedDescriptor,
            IReadOnlyList<BindingCandidateReport> candidates)
        {
            Property = property;
            IsLiveInstance = isLiveInstance;
            SelectedBindingType = selectedBindingType;
            ResolvedDescriptor = resolvedDescriptor;
            Candidates = candidates;
        }
    }
}