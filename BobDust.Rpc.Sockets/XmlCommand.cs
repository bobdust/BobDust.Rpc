using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using BobDust.Core.Extensions;

namespace BobDust.Rpc.Sockets
{
   public class XmlCommand : Command, ICommand
   {
      private class XmlNames
      {
         public const string Parameter = "Parameter";
         public const string Name = "name";
         public const string Type = "type";
      }

      public XmlCommand()
         : base()
      {
      }

      public XmlCommand(string operationName)
         : base(operationName)
      {
      }

      public override void Write(System.IO.BinaryWriter writer)
      {
         var builder = new StringBuilder();
         var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
         using (var xmlWriter = XmlWriter.Create(builder, settings))
         {
            xmlWriter.WriteStartElement(OperationName);
            foreach (var name in Parameters.Keys)
            {
               xmlWriter.WriteStartElement(XmlNames.Parameter);
               xmlWriter.WriteAttributeString(XmlNames.Name, name);
               var parameter = Parameters[name];
               xmlWriter.Write(parameter);
               xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
         }
         writer.Write(builder.ToString());
      }

      public override void Read(System.IO.BinaryReader reader)
      {
         var xml = reader.ReadString();
         using (var stringReader = new StringReader(xml))
         {
            using (var xmlReader = XmlReader.Create(stringReader))
            {
               xmlReader.Read();
               OperationName = xmlReader.Name;
               while (xmlReader.ReadToFollowing(XmlNames.Parameter))
               {
                  var name = xmlReader.GetAttribute(XmlNames.Name);
                  var type = Type.GetType(xmlReader.GetAttribute(XmlNames.Type));
                  xmlReader.Read();
                  var obj = xmlReader.Read(type);
                  Parameters[name] = obj;
               }
            }
         }
      }

      public override ICommandResult Return()
      {
         return new XmlCommandResult(OperationName);
      }

      public override ICommandResult Return(object value)
      {
         return new XmlCommandResult(OperationName, value);
      }

      public override ICommandResult Throw(Exception exception)
      {
         return new XmlCommandResult(OperationName, exception);
      }
   }
}
