using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spelunx
{
    [System.Serializable]
    public class CavernFeatureSet
    {
        [SerializeReference]
        public List<CavernFeature> cavernFeatures = new List<CavernFeature>();
        private CavernSetup cavernSetup;

        [Flags]
        internal enum DirtyState
        {
            None = 0,
            DirtyByFeatureChange = 1,
            DirtyByProfileReset = 2,
            Other = 4
        }

        internal DirtyState dirtyState;


        /// <summary>
        /// Adds a <see cref="CavernFeature"/> to this Cavern Feature Set.
        /// </summary>
        /// <remarks>
        /// You can only have a single feature of the same type per Cavern Feature Set.
        /// </remarks>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/>.</typeparam>
        /// <returns>The instance for the given type that you added to the Cavern Feature Set</returns>
        /// <seealso cref="Add"/>
        public T Add<T>()
            where T : CavernFeature
        {
            return (T)Add(typeof(T));
        }


        /// <summary>
        /// Adds a <see cref="CavernFeature"/> to this Cavern Feature Set.
        /// </summary>
        /// <remarks>
        /// You can only have a single feature of the same type per Cavern Feature Set.
        /// </remarks>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <returns>The instance created for the given type that has been added to the profile</returns>
        /// <seealso cref="Add{T}"/>
        public CavernFeature Add(Type type)
        {
            // if (Has(type))
            //     throw new InvalidOperationException("feature already exists in the volume");

            var feature = (CavernFeature)Activator.CreateInstance(type); //(CavernFeature)ScriptableObject.CreateInstance(type);
#if UNITY_EDITOR
            // feature.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            // feature.name = type.Name;
#endif
            // feature.SetCavern(cavernSetup);
            cavernFeatures.Add(feature);
            dirtyState |= DirtyState.DirtyByFeatureChange;
            return feature;
        }

        /// <summary>
        /// Removes a <see cref="CavernFeature"/> from this Cavern Feature Set.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the type does not exist in the Cavern Feature Set.
        /// </remarks>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/>.</typeparam>
        /// <seealso cref="Remove"/>
        public void Remove<T>()
            where T : CavernFeature
        {
            Remove(typeof(T));
        }

        /// <summary>
        /// Removes a <see cref="CavernFeature"/> from this Cavern Feature Set.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the type does not exist in the Cavern Feature Set.
        /// </remarks>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <seealso cref="Remove{T}"/>
        public void Remove(Type type)
        {
            int toRemove = -1;

            for (int i = 0; i < cavernFeatures.Count; i++)
            {
                if (cavernFeatures[i].GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove >= 0)
            {
                cavernFeatures.RemoveAt(toRemove);
                dirtyState |= DirtyState.DirtyByFeatureChange;
            }
        }

        /// <summary>
        /// Checks if this Cavern Feature Set contains the <see cref="CavernFeature"/> you pass in.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/>.</typeparam>
        /// <returns><c>true</c> if the <see cref="CavernFeature"/> exists in the Cavern Feature Set,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="Has"/>
        /// <seealso cref="HasSubclassOf"/>
        public bool Has<T>()
            where T : CavernFeature
        {
            return Has(typeof(T));
        }

        /// <summary>
        /// Checks if this Cavern Feature Set contains the <see cref="CavernFeature"/> you pass in.
        /// </summary>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <returns><c>true</c> if the <see cref="CavernFeature"/> exists in the Cavern Feature Set,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="Has{T}"/>
        /// <seealso cref="HasSubclassOf"/>
        public bool Has(Type type)
        {
            foreach (var feature in cavernFeatures)
            {
                if (feature.GetType() == type)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this Cavern Feature Set contains the <see cref="CavernFeature"/>, which is a subclass of <paramref name="type"/>,
        /// that you pass in.
        /// </summary>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <returns><c>true</c> if the <see cref="CavernFeature"/> exists in the Cavern Feature Set,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="Has"/>
        /// <seealso cref="Has{T}"/>
        public bool HasSubclassOf(Type type)
        {
            foreach (var feature in cavernFeatures)
            {
                if (feature.GetType().IsSubclassOf(type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="CavernFeature"/> of the specified type, if it exists.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/>.</typeparam>
        /// <param name="feature">The output argument that contains the <see cref="CavernFeature"/>
        /// or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="CavernFeature"/> is in the Cavern Feature Set,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(Type, out T)"/>
        /// <seealso cref="TryGetSubclassOf{T}"/>
        /// <seealso cref="TryGetAllSubclassOf{T}"/>
        public bool TryGet<T>(out T feature)
            where T : CavernFeature
        {
            return TryGet(typeof(T), out feature);
        }

        /// <summary>
        /// Gets the <see cref="CavernFeature"/> of the specified type, if it exists.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/></typeparam>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <param name="feature">The output argument that contains the <see cref="CavernFeature"/>
        /// or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="CavernFeature"/> is in the Cavern Feature Set,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(out T)"/>
        /// <seealso cref="TryGetSubclassOf{T}"/>
        /// <seealso cref="TryGetAllSubclassOf{T}"/>
        public bool TryGet<T>(Type type, out T feature)
            where T : CavernFeature
        {
            feature = null;

            foreach (var comp in cavernFeatures)
            {
                if (comp.GetType() == type)
                {
                    feature = (T)comp;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="CavernFeature"/>, which is a subclass of <paramref name="type"/>, if
        /// it exists.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/>.</typeparam>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <param name="feature">The output argument that contains the <see cref="CavernFeature"/>
        /// or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="CavernFeature"/> is in the Cavern Feature Set,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(Type, out T)"/>
        /// <seealso cref="TryGet{T}(out T)"/>
        /// <seealso cref="TryGetAllSubclassOf{T}"/>
        public bool TryGetSubclassOf<T>(Type type, out T feature)
            where T : CavernFeature
        {
            feature = null;

            foreach (var comp in cavernFeatures)
            {
                if (comp.GetType().IsSubclassOf(type))
                {
                    feature = (T)comp;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all the <see cref="CavernFeature"/> that are subclasses of the specified type,
        /// if there are any.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="CavernFeature"/>.</typeparam>
        /// <param name="type">A type that inherits from <see cref="CavernFeature"/>.</param>
        /// <param name="result">The output list that contains all the <see cref="CavernFeature"/>
        /// if any. Note that Unity does not clear this list.</param>
        /// <returns><c>true</c> if any <see cref="CavernFeature"/> have been found in the profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(Type, out T)"/>
        /// <seealso cref="TryGet{T}(out T)"/>
        /// <seealso cref="TryGetSubclassOf{T}"/>
        public bool TryGetAllSubclassOf<T>(Type type, List<T> result)
            where T : CavernFeature
        {
            if (cavernFeatures == null) return false;
            int count = result.Count;

            foreach (var comp in cavernFeatures)
            {
                if (comp.GetType().IsSubclassOf(type))
                    result.Add((T)comp);
            }

            return count != result.Count;
        }

        public List<T> GetAllOfType<T>()
        {
            if (cavernFeatures == null) return new List<T>();
            return cavernFeatures.OfType<T>().ToList();
        }

        /// <summary>
        /// Removes any features that were destroyed externally from the iternal list of features
        /// </summary>
        internal void Sanitize()
        {
            for (int i = cavernFeatures.Count - 1; i >= 0; i--)
                if (cavernFeatures[i] == null)
                    cavernFeatures.RemoveAt(i);
        }
    }
}
