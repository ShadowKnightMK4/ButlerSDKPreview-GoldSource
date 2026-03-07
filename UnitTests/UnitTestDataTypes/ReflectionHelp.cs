using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestDataTypes
{
    public static class ReflectionTestExtensions
    {
        public static T? GetPrivateField<T>(this object instance, string fieldName) where T : class
        {
            var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
                throw new Exception($"Field '{fieldName}' not found on type '{instance.GetType().Name}'");

            return fieldInfo.GetValue(instance) as T;
        }
    }
}
