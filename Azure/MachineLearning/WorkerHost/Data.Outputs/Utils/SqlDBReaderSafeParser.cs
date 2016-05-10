using System;
using System.Data.SqlClient;
using System.Reflection;

namespace WorkerHost.Data.Outputs.Utils
{
    public static class SqlDBReaderSafeParser
    {
        public static T SafeParse<T>(this SqlDataReader reader, string columnName, T defaultValue = default(T))
        {
            object dataValue = reader[columnName];
            T convertedValue = dataValue.SafeParse(defaultValue);

            return convertedValue;
        }

        public static T SafeParse<T>(this object dataValue, T defaultValue)
        {
            if (null == dataValue || DBNull.Value == dataValue)
                return defaultValue;

            Type t = typeof(T);

            if (t.IsInstanceOfType(dataValue))
            {
                T convertedValue = (T)dataValue;
                return convertedValue;
            }

            MethodInfo method = t.GetMethod("op_Implicit", new[] { dataValue.GetType() });

            T result;

            if (method != null)
            {
                result = (T)method.Invoke(null, new[] { dataValue });
            }
            else
            {
                result = defaultValue;
            }

            return result;
        }
    }
}
