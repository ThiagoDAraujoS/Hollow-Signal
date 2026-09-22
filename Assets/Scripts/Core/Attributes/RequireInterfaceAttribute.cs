using System;
using UnityEngine;

namespace Core.Attributes{
    /// Restricts a serialized MonoBehaviour field in the Inspector to components implementing the specified interface.
    public sealed class RequireInterfaceAttribute : PropertyAttribute{
        public readonly Type InterfaceType;

        /// Initializes attribute with required interface type.
        public RequireInterfaceAttribute(Type interfaceType) => InterfaceType = interfaceType;
    }
}
