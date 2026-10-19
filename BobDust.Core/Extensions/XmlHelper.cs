using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Reflection;

namespace BobDust.Core.Extensions
{
   public static class XmlHelper
   {
      private class XmlNames
      {
         public const string Item = "item";
         public const string Type = "type";
         public const string Nil = "nil";
         public const string ClassName = "ClassName";
         public const string Message = "Message";
         public const string StackTrace = "StackTrace";
         public const string InnerException = "InnerException";
      }

      public static object Deserialize(this XmlReader reader, Type type)
      {
         var serializer = new XmlSerializer(type);
         var obj = serializer.Deserialize(reader)!;
         return obj;
      }

      public static void Serialize(this XmlWriter writer, object? serializedObject)
      {
         var serializer = new XmlSerializer(serializedObject is null ? typeof(object) : serializedObject.GetType());
         //XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
         //namespaces.Add(string.Empty, string.Empty);
         serializer.Serialize(writer, serializedObject);
      }

      public static void WriteNil(this XmlWriter writer, string typeName)
      {
         var type = Type.GetType(typeName)!;
         writer.WriteAttributeString(XmlNames.Type, type.AssemblyQualifiedName);
         const string trueString = "true";
         writer.WriteAttributeString(XmlNames.Nil, trueString);
      }

      public static void Write(this XmlWriter writer, object obj)
      {
         var type = obj.GetType();
         var interfaces = type!.GetInterfaces();
#pragma warning disable SYSLIB0050 // Type or member is obsolete
         if (type.IsValueType || type.IsPrimitive || type == typeof(string))
         {
            writer.WriteAttributeString(XmlNames.Type, type.AssemblyQualifiedName);
            writer.WriteValue(obj);
         }
         else if (!type.IsGenericType && type.IsSerializable)
         {
            writer.WriteAttributeString(XmlNames.Type, type.AssemblyQualifiedName);
            writer.Serialize(obj);
         }
         else if (interfaces.Contains(typeof(IEnumerable)))
         {
            var collection = (obj as IEnumerable)!;
            var enumerator = collection.GetEnumerator();
            if (enumerator.MoveNext())
            {
               var current = enumerator.Current;
               var itemType = current.GetType();
               if (itemType.IsSerializable)
               {
                  var newType = typeof(List<>).MakeGenericType(itemType);
                  writer.WriteAttributeString(XmlNames.Type, newType.AssemblyQualifiedName);

                  writer.WriteStartElement(XmlNames.Item);
                  writer.Serialize(current);
                  writer.WriteEndElement();
                  while (enumerator.MoveNext())
                  {
                     var item = enumerator.Current;
                     writer.WriteStartElement(XmlNames.Item);
                     writer.Serialize(item);
                     writer.WriteEndElement();
                  }
               }
            }
            else if (type.IsGenericType)
            {
               var newType = typeof(List<>).MakeGenericType(type.GetGenericArguments().First());
               writer.WriteAttributeString(XmlNames.Type, newType.AssemblyQualifiedName);
            }
            else
            {
               writer.WriteAttributeString(XmlNames.Type, type.AssemblyQualifiedName);
            }
         }
#pragma warning restore SYSLIB0050 // Type or member is obsolete
      }

      public static object Read(this XmlReader reader, Type type)
      {
         var interfaces = type.GetInterfaces();
#pragma warning disable SYSLIB0050 // Type or member is obsolete
         if (type.IsValueType || type.IsPrimitive || type == typeof(string))
         {
            var value = reader.ReadString();
            return Convert.ChangeType(value, type);
         }
         else if (!type.IsGenericType && type.IsSerializable)
         {
            var obj = reader.Deserialize(type);
            return obj;
         }
         else if (interfaces.Contains(typeof(IEnumerable)))
         {
            var collection = type.GetConstructor(Type.EmptyTypes)!.Invoke(Enumerable.Empty<object>().ToArray());
            var itemType = type.GetGenericArguments().FirstOrDefault();
            while (reader.IsStartElement(XmlNames.Item) || reader.ReadToFollowing(XmlNames.Item))
            {
               reader.Read();
               var item = reader.Deserialize(itemType!);
               ((IList)collection).Add(item);
            }
            return collection;
         }
#pragma warning restore SYSLIB0050 // Type or member is obsolete
         return null!;
      }

      public static void WriteException(this XmlWriter writer, Exception exception)
      {
         writer.WriteAttributeString(XmlNames.ClassName, exception.GetType().FullName);
         writer.WriteAttributeString(XmlNames.Message, exception.Message);
         writer.WriteAttributeString(XmlNames.StackTrace, exception.StackTrace);

         if (exception is { InnerException: { } inner })
         {
            writer.WriteStartElement(XmlNames.InnerException);
            writer.WriteException(inner);
            writer.WriteEndElement();
         }
      }

      public static Exception ReadException(this XmlReader reader, Type? exceptionType = default)
      {
         if (exceptionType == default)
         {
            var typeName = reader.GetAttribute(XmlNames.ClassName);
            exceptionType = Type.GetType(typeName!);
         }
         var message = reader.GetAttribute(XmlNames.Message);
         var stackTrace = reader.GetAttribute(XmlNames.StackTrace);
         reader.Read();
         Exception? inner = default;
         if (reader.IsStartElement(XmlNames.InnerException))
         {
            inner = reader.ReadException();
         }
         // Fallback to standard Exception if the specific type isn't found
         exceptionType ??= typeof(Exception);

         // 2. Instantiate using the string message constructor
         Exception? exception = Activator.CreateInstance(exceptionType, message, inner) as Exception
             ?? new Exception(message, inner);

         // 3. Attach the arbitrary text stack trace string cleanly
         if (!string.IsNullOrWhiteSpace(stackTrace))
         {
#if !NET48
            // SetRemoteStackTrace populates the StackTrace property with a high fidelity raw string
            ExceptionDispatchInfo.SetRemoteStackTrace(exception, stackTrace);
#else
         var stackTraceStringField = exception.GetType().GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
         stackTraceStringField?.SetValue(exception, stackTrace);
#endif
         }
         return exception;
      }
   }
}
