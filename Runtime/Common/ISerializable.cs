using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Sindy.Common
{
    public interface ISerializable<KeyType, ValueType>
    {
        /// <summary>
        /// Serializes the object to a string.
        /// </summary>
        /// <returns>A string representation of the serialized object.</returns>
        JContainer Serialize();

        /// <summary>
        /// Deserializes the object from a string.
        /// </summary>
        /// <param name="data">The string data to deserialize from.</param>
        /// <param name="refs">A dictionary of references to resolve any dependencies during deserialization.</param>
        void Deserialize(JContainer data, Dictionary<KeyType, ValueType> refs);
    }
}
