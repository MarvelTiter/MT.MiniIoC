using CommonServiceLocator;
using MT.MiniIoc.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MT.MiniIoc {

    public partial class MIoC : IMIocContainter, IServiceLocator {

        private readonly string _defaultKey = Guid.NewGuid().ToString();
        private static readonly object _instanceLock = new object();
        private readonly object _syncLock = new object();

        private Dictionary<Type, Type> _interfaceClassMapper = new Dictionary<Type, Type>();
        private Dictionary<Type, ConstructorInfo> _ctorInfos = new Dictionary<Type, ConstructorInfo>();
        private Dictionary<Type, Dictionary<string, Delegate>> _factories = new Dictionary<Type, Dictionary<string, Delegate>>();
        private Dictionary<Type, Dictionary<string, object>> _cacheInstance = new Dictionary<Type, Dictionary<string, object>>();
        private Dictionary<Type, bool> _cacheType = new Dictionary<Type, bool>();


        private static MIoC _default;
        public static MIoC Default {
            get {
                if (_default == null) {
                    lock (_instanceLock) {
                        if (_default == null) {
                            _default = new MIoC();
                        }
                    }
                }

                return _default;
            }
        }

        public IMIocContainter RegisterScope<T>() where T : class {
            Register<T>(false, false, null);
            return this;
        }

        public IMIocContainter RegisterScope(Type classType) {
            Register(classType, false, false, null);
            return this;
        }

        public IMIocContainter RegisterScope<TI, T>()
            where TI : class
            where T : class, TI {
            Register<TI, T>(false, false, null);
            return this;
        }
        public IMIocContainter RegisterSingle(Type classType, bool createImmediately, string key = null) {
            Register(classType, true, createImmediately, key);
            return this;
        }
        public IMIocContainter RegisterSingle<T>(bool createImmediately, string key = null)
            where T : class {
            Register<T>(true, createImmediately, key);
            return this;
        }

        public IMIocContainter RegisterSingle<TI, T>(bool createImmediately, string key = null)
            where TI : class
            where T : class, TI {
            Register<TI, T>(true, createImmediately, key);
            return this;
        }

        public IMIocContainter RegisterSingle<T>(Func<T> factory, string key = null)
            where T : class {
            Register<T>(true, true, key, factory);
            return this;
        }

        public void UnRegister<T>(string key = null) {
            lock (_syncLock) {
                Type typeFromHandle = typeof(T);
                Type keyType = (!_interfaceClassMapper.ContainsKey(typeFromHandle)) ? typeFromHandle : (_interfaceClassMapper[typeFromHandle] ?? typeFromHandle);
                if (key != null) {
                    UnRegister(typeFromHandle, key);
                    return;
                }
                if (_cacheInstance.ContainsKey(typeFromHandle)) {
                    _cacheInstance.Remove(typeFromHandle);
                }

                if (_interfaceClassMapper.ContainsKey(typeFromHandle)) {
                    _interfaceClassMapper.Remove(typeFromHandle);
                }

                if (_factories.ContainsKey(typeFromHandle)) {
                    _factories.Remove(typeFromHandle);
                }

                if (_ctorInfos.ContainsKey(keyType)) {
                    _ctorInfos.Remove(keyType);
                }
            }
        }

        private void UnRegister(Type typeFromHandle, string key) {
            if (_cacheInstance.ContainsKey(typeFromHandle) && _cacheInstance[typeFromHandle].ContainsKey(key))
                _cacheInstance[typeFromHandle].Remove(key);

            if (_factories.ContainsKey(typeFromHandle) && _factories[typeFromHandle].ContainsKey(key))
                _factories[typeFromHandle].Remove(key);
        }


        #region IServiceLocator member

        public object GetInstance(Type serviceType) {
            return DoGetService(serviceType, _defaultKey);
        }

        public object GetInstance(Type serviceType, string key) {
            return DoGetService(serviceType, key);
        }

        public IEnumerable<object> GetAllInstances(Type serviceType) {
            throw new NotImplementedException();
        }

        public TService GetInstance<TService>() {
            return (TService)DoGetService(typeof(TService), _defaultKey);
        }

        public TService GetInstance<TService>(string key) {
            return (TService)DoGetService(typeof(TService), key);
        }

        public IEnumerable<TService> GetAllInstances<TService>() {
            throw new NotImplementedException();
        }

        object IServiceProvider.GetService(Type serviceType) {
            return GetInstance(serviceType);
        }


        #endregion

    }

    /// <summary>
    /// private member
    /// </summary>
    public partial class MIoC {
        private void Register<TI, T>(bool single, bool createImmediately, string key)
            where TI : class
            where T : class, TI {
            lock (_syncLock) {
                var interfaceType = typeof(TI);
                var classType = typeof(T);

                if (_interfaceClassMapper.ContainsKey(interfaceType)) {
                    if (_interfaceClassMapper[interfaceType] != classType) {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "There is already a class registered for {0}.",
                                interfaceType.FullName));
                    }
                } else {
                    _interfaceClassMapper.Add(interfaceType, classType);
                    _ctorInfos.Add(classType, GetConstructorInfo(classType));
                }
                Func<TI> factory = MakeInstance<TI>;
                DoRegister(interfaceType, factory, key ?? _defaultKey);

                if (!_cacheType.ContainsKey(interfaceType))
                    _cacheType.Add(interfaceType, single);

                if (createImmediately) {
                    CacheInstance(interfaceType, key ?? _defaultKey);
                }
            }
        }

        private void Register<T>(bool single, bool createImmediately, string key, Func<T> fac = null) {
            var classType = typeof(T);

            if (classType.IsInterface) {
                throw new ArgumentException("An interface cannot be registered alone.");
            }

            lock (_syncLock) {
                if (_factories.ContainsKey(classType)
                    && _factories[classType].ContainsKey(_defaultKey)) {
                    if (!_ctorInfos.ContainsKey(classType)) {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                            "Class {0} is already registered.",
                            classType));
                    }
                    return;
                }

                if (!_interfaceClassMapper.ContainsKey(classType)) {
                    _interfaceClassMapper.Add(classType, null);
                    _ctorInfos.Add(classType, GetConstructorInfo(classType));
                }

                Func<T> factory = fac ?? MakeInstance<T>;

                DoRegister(classType, factory, key ?? _defaultKey);

                if (!_cacheType.ContainsKey(classType))
                    _cacheType.Add(classType, single);

                if (createImmediately) {
                    CacheInstance(classType, key ?? _defaultKey);
                }
            }
        }
        private void Register(Type classType, bool single, bool createImmediately, string key) {

            if (classType.IsInterface) {
                throw new ArgumentException("An interface cannot be registered alone.");
            }

            lock (_syncLock) {
                if (_factories.ContainsKey(classType)
                    && _factories[classType].ContainsKey(_defaultKey)) {
                    if (!_ctorInfos.ContainsKey(classType)) {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                            "Class {0} is already registered.",
                            classType));
                    }
                    return;
                }

                if (!_interfaceClassMapper.ContainsKey(classType)) {
                    _interfaceClassMapper.Add(classType, null);
                    _ctorInfos.Add(classType, GetConstructorInfo(classType));
                }

                Func<object> factory = () => MakeInstance(classType);

                DoRegister(classType, factory, key ?? _defaultKey);

                if (!_cacheType.ContainsKey(classType))
                    _cacheType.Add(classType, single);

                if (createImmediately) {
                    CacheInstance(classType, key ?? _defaultKey);
                }
            }
        }
        private T MakeInstance<T>() {
            var serviceType = typeof(T);
            var constructor = _ctorInfos.ContainsKey(serviceType)
                                  ? _ctorInfos[serviceType]
                                  : GetConstructorInfo(serviceType);

            var parameterInfos = constructor.GetParameters();

            if (parameterInfos.Length == 0) {
                return (T)constructor.Invoke(null);
            }

            var parameters = new object[parameterInfos.Length];

            foreach (var parameterInfo in parameterInfos) {
                parameters[parameterInfo.Position] = GetService(parameterInfo.ParameterType);
            }

            return (T)constructor.Invoke(parameters);
        }
        private object MakeInstance(Type serviceType) {
            var constructor = _ctorInfos.ContainsKey(serviceType)
                                  ? _ctorInfos[serviceType]
                                  : GetConstructorInfo(serviceType);

            var parameterInfos = constructor.GetParameters();

            if (parameterInfos.Length == 0) {
                return constructor.Invoke(null);
            }

            var parameters = new object[parameterInfos.Length];

            foreach (var parameterInfo in parameterInfos) {
                parameters[parameterInfo.Position] = GetService(parameterInfo.ParameterType);
            }

            return constructor.Invoke(parameters);
        }
        private ConstructorInfo GetConstructorInfo(Type serviceType) {
            Type resolveTo;

            if (_interfaceClassMapper.ContainsKey(serviceType)) {
                resolveTo = _interfaceClassMapper[serviceType] ?? serviceType;
            } else {
                resolveTo = serviceType;
            }

            var constructorInfos = resolveTo.GetConstructors();

            if (constructorInfos.Length == 0 || (constructorInfos.Length == 1 && !constructorInfos[0].IsPublic)) {
                throw new Exception(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot register: No public constructor found in {0}.",
                        resolveTo.Name));
            }
            if (constructorInfos.Length > 1) {
                if (constructorInfos.Length > 2) {
                    return GetPreferredConstructorInfo(constructorInfos);
                }

                if (constructorInfos.FirstOrDefault(i => i.Name == ".cctor") == null) {
                    return GetPreferredConstructorInfo(constructorInfos);
                }

                var first = constructorInfos.FirstOrDefault(i => i.Name != ".cctor");

                if (first == null || !first.IsPublic) {
                    throw new Exception(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cannot register: No public constructor found in {0}.",
                            resolveTo.Name));
                }
                return first;
            }
            return constructorInfos[0];
        }
        private ConstructorInfo GetPreferredConstructorInfo(IEnumerable<ConstructorInfo> ctors) {
            //ctors.Where(c => {
            //    c.GetCustomAttribute(typeof)
            //})

            throw new Exception(string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot register: NotImplemented"));
        }
        private void DoRegister<T>(Type classType, Func<T> factory, string key) {
            if (_factories.ContainsKey(classType)) {
                if (_factories[classType].ContainsKey(key)) {
                    return;
                }
                _factories[classType].Add(key, factory);
            } else {
                var list = new Dictionary<string, Delegate>();
                list.Add(key, factory);
                _factories.Add(classType, list);
            }
        }
        private object GetService(Type serviceType) {
            return DoGetService(serviceType, _defaultKey);
        }
        private object DoGetService(Type serviceType, string key) {
            lock (_syncLock) {
                if (string.IsNullOrEmpty(key)) {
                    key = _defaultKey;
                }
                var cache = _cacheType.ContainsKey(serviceType) && _cacheType[serviceType];
                if (cache)
                    return CacheInstance(serviceType, key);
                else
                    return CreateInstance(serviceType, key);

            }
        }
        private object CreateInstance(Type serviceType, string key) {
            if (_factories.ContainsKey(serviceType)) {
                if (_factories[serviceType].ContainsKey(key)) {
                    return _factories[serviceType][key].DynamicInvoke(null);
                }

                if (_factories[serviceType].ContainsKey(_defaultKey)) {
                    return _factories[serviceType][_defaultKey].DynamicInvoke(null);
                }
            }
            throw new Exception(
                string.Format("Type not found: {0}", serviceType.FullName));

        }
        private object CacheInstance(Type serviceType, string key) {
            Dictionary<string, object> instances = null;
            if (!_cacheInstance.ContainsKey(serviceType)) {
                if (!_interfaceClassMapper.ContainsKey(serviceType)) {
                    throw new Exception(
                        string.Format("Type not found in cache: {0}.", serviceType.FullName));
                }
                instances = new Dictionary<string, object>();
                _cacheInstance.Add(serviceType, instances);
            } else {
                instances = _cacheInstance[serviceType];
            }

            if (instances != null
                && instances.ContainsKey(key)) {
                return instances[key];
            }
            var instance = CreateInstance(serviceType, key);

            if (instances != null) {
                instances.Add(key, instance);
            }
            return instance;
        }
    }
}
