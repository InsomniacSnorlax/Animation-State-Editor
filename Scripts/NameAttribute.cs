using System;
using UnityEngine;
namespace Snorlax.AnimationHash
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class NameAttribute : PropertyAttribute
    {
        public string AttributeName { get; private set; }

        public NameAttribute(string name)
        {
            AttributeName = name;
        }
    }
}