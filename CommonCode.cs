using R2API;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ThinkInvisible.Admiral {
    public static class CommonCode {
        /// <summary>
        /// Loads a prefab from RoR2 addressable assets, clones it without awakening it, applies a modifier function to the clone, then performs a second InstantiateClone operation to freeze the modified version into a new named prefab.
        /// </summary>
        public static GameObject ModifyVanillaPrefab(string addressablePath, string newName, bool shouldNetwork, Func<GameObject, GameObject> modifierCallback) {
            var origObj = Addressables.LoadAssetAsync<GameObject>(addressablePath)
                .WaitForCompletion()
                .InstantiateClone("Temporary Setup Prefab", false);
            var newObj = modifierCallback(origObj);
            var newObjPrefabified = newObj.InstantiateClone(newName, shouldNetwork);
            GameObject.Destroy(origObj);
            GameObject.Destroy(newObj);
            return newObjPrefabified;
        }

        /// <summary>
        /// Wraps a float within the bounds of two other floats.
        /// </summary>
        /// <param name="x">The number to perform a wrap operation on.</param>
        /// <param name="min">The lower bound of the wrap operation.</param>
        /// <param name="max">The upper bound of the wrap operation.</param>
        /// <returns>The result of the wrap operation of x within min, max.</returns>
        public static float Wrap(float x, float min, float max) {
            if(x < min)
                return max - (min - x) % (max - min);
            else
                return min + (x - min) % (max - min);
        }
    }
}
