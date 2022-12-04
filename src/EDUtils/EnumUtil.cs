using System.Reflection;
using System.Runtime.Serialization;

namespace EDUtils
{
    public static class EnumUtil
    {
        public static string GetEnumMemberValue<T>(this T value) where T : Enum
        {
            if (typeof(T).GetTypeInfo().DeclaredMembers.SingleOrDefault(x => x.Name == value.ToString())?.GetCustomAttribute<EnumMemberAttribute>(false) is EnumMemberAttribute enumMemberAttribute &&
                !string.IsNullOrEmpty(enumMemberAttribute.Value))
            {
                return enumMemberAttribute.Value;
            }
            return value.ToString();
        }
    }
}