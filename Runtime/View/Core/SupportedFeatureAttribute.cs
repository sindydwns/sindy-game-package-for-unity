using System;

namespace Sindy.View
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class SupportedFeatureAttribute : Attribute
    {
        public Type FeatureType { get; }

        public SupportedFeatureAttribute(Type featureType)
        {
            if (featureType == null)
                throw new ArgumentNullException(nameof(featureType));
            if (!typeof(ModelFeature).IsAssignableFrom(featureType))
                throw new ArgumentException($"{featureType.Name}은 ModelFeature를 상속받지 않습니다.", nameof(featureType));
            FeatureType = featureType;
        }
    }
}
