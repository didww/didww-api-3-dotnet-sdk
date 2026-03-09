using Didww.Api3.Resource;
using JsonApiSerializer.ContractResolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Didww.Api3.Converter;

public class DirtyContractResolver : JsonApiContractResolver
{
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (typeof(BaseResource).IsAssignableFrom(property.DeclaringType))
        {
            var baseShouldSerialize = property.ShouldSerialize;
            var valueProvider = property.ValueProvider;

            property.ShouldSerialize = instance =>
            {
                // Let base decide first
                if (baseShouldSerialize != null && !baseShouldSerialize(instance))
                    return false;

                if (instance is not BaseResource resource)
                    return true;

                if (!DirtySerializationContext.IsDirtyOnlyModeEnabled)
                {
                    // In normal mode (create), skip null values
                    var value = valueProvider?.GetValue(instance);
                    if (value == null)
                        return false;
                    return true;
                }

                // In dirty-only mode, only serialize id + type + dirty fields
                if (property.PropertyName == "id" || property.PropertyName == "type")
                    return true;

                return resource.IsFieldDirty(property.PropertyName!);
            };

            // Include null values — we handle null filtering in ShouldSerialize above
            property.NullValueHandling = NullValueHandling.Include;
        }

        return property;
    }
}
