using System;
using UnityEngine;

namespace Funder.Core.Singleton
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SingletonAttribute : Attribute
    {
        public string Name { get; set; }
        public HideFlags HideFlags { get; set; }
        public bool Automatic { get; set; }
        public bool Persistent { get; set; }
        public bool RemoveDuplicates { get; set; }

        public SingletonAttribute()
        {
            HideFlags = HideFlags.None;
            Automatic = true;
            Persistent = true;
            RemoveDuplicates = true;
        }
    }
}
