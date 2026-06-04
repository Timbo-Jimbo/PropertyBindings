using TimboJimbo.Core.Utility;
using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    /// <summary>
    /// Base class for optimized read/write property bindings. Implements caching and change detection logic to minimize expensive read/write operations on the target object.
    /// Only takes effect for 'live' instances (i.e. when the target object is part of an active scene). 
    /// For non-live instances (e.g. prefabs, edit-time objects), it always reads/writes directly to the target to ensure correct behavior in the editor.
    /// </summary>
    public abstract class OptimizedReadWritePropertyBinding : IPropertyBinding
    {
        protected ValueContainer _valueCache;
        private bool _valueCacheInitialized;
        private OptimizationConfig _optimizationConfig;
        private bool _isLiveInstance;

        protected OptimizedReadWritePropertyBinding(GameObject root, OptimizationConfig optimizationConfig)
        {
            _isLiveInstance = EditorAwareUtility.IsLiveInstance(root);
            _optimizationConfig = optimizationConfig;
        }
        
        public abstract void Dispose();

        protected abstract bool TargetMustBeNotifiedOnWrite();
        protected bool TargetIsConsideredLiveInstance() => _isLiveInstance;

        public ReadResult Read()
        {
            var readMode = _isLiveInstance ? _optimizationConfig.ReadStratergy : ReadStratergy.AlwaysReadFromTarget;

            switch (readMode)
            {
                case ReadStratergy.AlwaysReadFromTarget:
                {
                    var readSuccessful = TryReadFromTarget(out var valueContainer);
                    return new () { Success = readSuccessful, Value = valueContainer };
                }
                
                case ReadStratergy.ReadOnceAndCache:
                {
                    if (!_valueCacheInitialized)
                    {
                        var readSuccessful = TryReadFromTarget(out _valueCache);

                        if (readSuccessful)
                        {
                            _valueCacheInitialized = true;
                        }
                        else
                        {
                            return new () { Success = false };
                        }
                    }

                    return new ReadResult() { Success = true, Value = _valueCache };
                }

                default:
                    throw new System.InvalidOperationException($"Unsupported ReadMode: {readMode}");
            }
        }

        public WriteResult Write(ValueContainer valueContainer)
        {
            var writeMode = _isLiveInstance ? _optimizationConfig.WriteStratergy : WriteStratergy.AlwaysWriteToTarget;

            switch (writeMode)
            {
                case WriteStratergy.AlwaysWriteToTarget:
                {
                    var writeSuccessful = TryWriteToTarget(valueContainer);

                    if (!writeSuccessful)
                        return new () { Success = false };
                    
                    break;
                }

                case WriteStratergy.WriteOnValueChange:
                {
                    var readResult = Read();

                    if (!readResult.Success)
                        return new () { Success = false };

                    if (readResult.Value.Equals(valueContainer))
                        return new () { Success = true, NotifyTarget = false };

                    var writeResult = TryWriteToTarget(valueContainer);

                    if (!writeResult)
                        return new () { Success = false };
                        
                    break;
                }

                case WriteStratergy.WriteOnApproxValueChange:
                {
                    var readResult = Read();

                    if (!readResult.Success)
                        return new () { Success = false };

                    if (readResult.Value.ApproximatelyEquals(valueContainer))
                        return new () { Success = true, NotifyTarget = false };

                    var writeResult = TryWriteToTarget(valueContainer);

                    if (!writeResult)
                        return new () { Success = false };
                    
                    break;
                }

                default:
                    throw new System.InvalidOperationException($"Unsupported WriteMode: {writeMode}");
            }
            
            // if we reached this point, then we've performed a 'real' write to the target
            // so lets cache the result, and raise notificaitons if needed
            _valueCache = valueContainer;
            _valueCacheInitialized = true;
            return new () { Success = true, NotifyTarget = TargetMustBeNotifiedOnWrite() };
        }

        protected abstract bool TryReadFromTarget(out ValueContainer valueContainer);
        
        protected abstract bool TryWriteToTarget(ValueContainer valueContainer);
        
        protected struct OptimizationConfig
        {
            public ReadStratergy ReadStratergy;
            public WriteStratergy WriteStratergy;

            public static OptimizationConfig Aggressive => new OptimizationConfig()
            {
                ReadStratergy = ReadStratergy.ReadOnceAndCache,
                WriteStratergy = WriteStratergy.WriteOnApproxValueChange
            };

            public static OptimizationConfig Moderate => new OptimizationConfig()
            {
                ReadStratergy = ReadStratergy.ReadOnceAndCache,
                WriteStratergy = WriteStratergy.WriteOnValueChange
            };

            public static OptimizationConfig None => new OptimizationConfig()
            {
                ReadStratergy = ReadStratergy.AlwaysReadFromTarget,
                WriteStratergy = WriteStratergy.AlwaysWriteToTarget
            };
        }

        protected enum ReadStratergy
        {
            AlwaysReadFromTarget,
            ReadOnceAndCache,
        }

        protected enum WriteStratergy
        {
            AlwaysWriteToTarget,
            WriteOnValueChange,
            WriteOnApproxValueChange,
        }
    }
}
