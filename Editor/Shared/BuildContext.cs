using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of IBuildContext. Stores data generated during a build.
    /// <seealso cref="IBuildContext"/>
    /// </summary>
    public class BuildContext : IBuildContext
    {
        internal Dictionary<Type, IContextObject> m_ContextObjects;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BuildContext()
        {
            m_ContextObjects = new Dictionary<Type, IContextObject>();
        }

        /// <summary>
        /// Default constructor, adds the passed in parameters to the context.
        /// </summary>
        /// <param name="buildParams">The set of initial parameters to add to the context.</param>
        public BuildContext(params IContextObject[] buildParams)
        {
            m_ContextObjects = new Dictionary<Type, IContextObject>();

            if (buildParams == null)
                return;

            foreach (var buildParam in buildParams)
            {
                if (buildParam != null)
                    SetContextObject(buildParam);
            }
        }

        /// <inheritdoc />
        public void SetContextObject<T>(IContextObject contextObject) where T : IContextObject
        {
            if (contextObject == null)
                throw new ArgumentNullException(nameof(contextObject));

            var type = typeof(T);
            if (!type.IsInterface)
                throw new InvalidOperationException($"Passed in type '{type}' is not an interface.");
            
            if (contextObject is not T)
                throw new InvalidOperationException($"'{contextObject.GetType()}' is not of passed in type '{type}'.");
            
            m_ContextObjects[typeof(T)] = contextObject;
        }

        /// <inheritdoc />
        public void SetContextObject(Type type, IContextObject contextObject)
        {
            if (contextObject == null)
                throw new ArgumentNullException(nameof(contextObject));

            if (!type.IsInterface)
                throw new InvalidOperationException($"Passed in type '{type}' is not an interface.");
            
            if (!type.IsInstanceOfType(contextObject))
                throw new InvalidOperationException($"'{contextObject.GetType()}' is not of passed in type '{type}'.");
            
            m_ContextObjects[type] = contextObject;
        }

        private IEnumerable<Type> WalkAssignableTypes(IContextObject contextObject)
        {
            Type iCType = typeof(IContextObject);
            
            // 获取所有继承自 IContextObject 的接口 (非 IContextObject)
            foreach (Type t in contextObject.GetType().GetInterfaces())
            {
                if (iCType.IsAssignableFrom(t) && t != iCType)
                    yield return t;
            }

            // 递归继承链，获取所有继承自 IContextObject 的父类 (非 IContextObject)
            for (Type current = contextObject.GetType(); current != null; current = current.BaseType)
                if (iCType.IsAssignableFrom(current) && current != iCType)
                    yield return current;
        }

        /// <inheritdoc />
        public void SetContextObject(IContextObject contextObject)
        {
            if (contextObject == null)
                throw new ArgumentNullException(nameof(contextObject));

            List<Type> types = new List<Type>(WalkAssignableTypes(contextObject));
            if (types.Count == 0)
                throw new Exception($"Could not determine context object type for object of type {contextObject.GetType().FullName}");
            
            types.ForEach(x => m_ContextObjects[x] = contextObject);
        }

        /// <inheritdoc />
        public bool ContainsContextObject<T>() where T : IContextObject
        {
            return ContainsContextObject(typeof(T));
        }

        /// <inheritdoc />
        public bool ContainsContextObject(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return m_ContextObjects.ContainsKey(type);
        }

        /// <inheritdoc />
        public T GetContextObject<T>() where T : IContextObject
        {
            return (T)GetContextObject(typeof(T));
        }

        /// <inheritdoc />
        public IContextObject GetContextObject(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!m_ContextObjects.ContainsKey(type))
                throw new Exception($"Object of Type {type} was not available within the BuildContext");

            return m_ContextObjects[type];
        }

        /// <inheritdoc />
        public bool TryGetContextObject<T>(out T contextObject) where T : IContextObject
        {
            IContextObject cachedContextObject;
            if (m_ContextObjects.TryGetValue(typeof(T), out cachedContextObject) && cachedContextObject is T)
            {
                contextObject = (T)cachedContextObject;
                return true;
            }

            contextObject = default(T);
            return false;
        }

        /// <inheritdoc />
        public bool TryGetContextObject(Type type, out IContextObject contextObject)
        {
            IContextObject cachedContextObject;
            if (m_ContextObjects.TryGetValue(type, out cachedContextObject) && type.IsInstanceOfType(cachedContextObject))
            {
                contextObject = cachedContextObject;
                return true;
            }

            contextObject = null;
            return false;
        }
    }
}
