using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BobDust.Core.Extensions
{
    public static class XmlHelper
   {
      private class XmlNames
      {
         public const string Item = "item";
         public const string Type = "type";
      }

      public static object Deserialize(this XmlReader reader, Type type)
      {
         var serializer = new XmlSerializer(type);
         var obj = serializer.Deserialize(reader);
         return obj;
      }

      public static void Serialize(this XmlWriter writer, object serializedObject)
      {
         var serializer = new XmlSerializer(serializedObject.GetType());
         //XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
         //namespaces.Add(string.Empty, string.Empty);
         serializer.Serialize(writer, serializedObject);
      }

      public static void Write(this XmlWriter writer, object obj)
      {
         var type = obj.GetType();
         var interfaces = type.GetInterfaces();
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
            var collection = obj as IEnumerable;
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
                  while(enumerator.MoveNext())
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
               var newType = typeof(List<>).MakeGenericType(type.GetGenericArguments().FirstOrDefault());
               writer.WriteAttributeString(XmlNames.Type, newType.AssemblyQualifiedName);
            }
            else
            {
               writer.WriteAttributeString(XmlNames.Type, type.AssemblyQualifiedName);
            }
         }
      }

      public static object Read(this XmlReader reader, Type type)
      {
         var interfaces = type.GetInterfaces();
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
            var collection = type.GetConstructor(Type.EmptyTypes).Invoke(Enumerable.Empty<object>().ToArray());
            var itemType = type.GetGenericArguments().FirstOrDefault();
            while (reader.IsStartElement(XmlNames.Item) || reader.ReadToFollowing(XmlNames.Item))
            {
               reader.Read();
               var item = reader.Deserialize(itemType);
               ((IList)collection).Add(item);
            }
            return collection;
         }
         return null;
      }

      public static void WriteBinary(this XmlWriter writer, object obj)
      {
         using (var stream = new MemoryStream())
         {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            var bytes = stream.ToArray();
            var text = Convert.ToBase64String(bytes);
            writer.WriteString(text);
         }
      }

      public static object ReadBinary(this XmlReader reader)
      {
         var text = reader.ReadString();
         var bytes = Convert.FromBase64String(text);
         using (var stream = new MemoryStream(bytes))
         {
            var formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
         }
      }

   }
}
