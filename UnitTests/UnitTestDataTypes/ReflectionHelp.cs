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
        /// <summary>
        /// pull a privat efield from an instance, searching up the inheritance chain if necessary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T? GetPrivateField<T>(this object instance, string fieldName) where T : class
        {
            var TypeWalker = instance.GetType();
        base_walk:
            
            var DebugMethod = TypeWalker.GetProperties();
            var fieldInfo = TypeWalker.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                TypeWalker = TypeWalker.BaseType;
                if (TypeWalker is null)
                {
                    throw new Exception($"Field '{fieldName}' not found on type '{instance.GetType().Name}'");
                }
                else
                {
                    goto base_walk;
                }
            }
            return fieldInfo.GetValue(instance) as T;
        }
    }
}
