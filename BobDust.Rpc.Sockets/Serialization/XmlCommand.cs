using System;
using System.Text;
using System.Xml;
using System.IO;
using BobDust.Core.Extensions;
using BobDust.Rpc.Sockets.Abstractions;
using System.Collections.Generic;

namespace BobDust.Rpc.Sockets.Serialization
{
	class XmlCommand : Command, ICommand
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

		public XmlCommand(string operationName, IEnumerable<(string Name, object Value)> parameters)
		   : base(operationName)
		{
			Parameters = parameters;
		}

		public override void Write(System.IO.BinaryWriter writer)
		{
			var builder = new StringBuilder();
			var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
			using (var xmlWriter = XmlWriter.Create(builder, settings))
			{
				xmlWriter.WriteStartElement(OperationName);
				foreach (var parameter in Parameters)
				{
					var name = parameter.Name;
					xmlWriter.WriteStartElement(XmlNames.Parameter);
					xmlWriter.WriteAttributeString(XmlNames.Name, name);
					var value = parameter.Value;
					xmlWriter.Write(value);
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
					var paramList = new List<(string Name, object Value)>();
					while (xmlReader.ReadToFollowing(XmlNames.Parameter))
					{
						var name = xmlReader.GetAttribute(XmlNames.Name);
						var type = Type.GetType(xmlReader.GetAttribute(XmlNames.Type));
						xmlReader.Read();
						var obj = xmlReader.Read(type);
						paramList.Add((name, obj));
					}
					Parameters = paramList;
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
