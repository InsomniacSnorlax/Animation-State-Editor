using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snorlax.AnimationHash
{
    public class HashKeys : ScriptableObject
    {
        public RuntimeAnimatorController animatorController;
        public Key[] Keys = new Key[0];
    }

    [System.Serializable]
    public struct Key
    {
        public string Name;
        public AnimationClip Clip;
        public int LayerIndex;
        public int StateIndex;
    }
}