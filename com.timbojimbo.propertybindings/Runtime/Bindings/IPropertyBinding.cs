using System;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public interface IPropertyBinding : IDisposable
    {
        ReadResult Read();
        WriteResult Write(ValueContainer valueContainer);
    }

    public struct ReadResult
    {
        public bool Success;
        public ValueContainer Value;
    }

    public struct WriteResult
    {
        public bool Success;
        public bool NotifyTarget;
    }
}