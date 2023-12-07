using System;
using System.Text;
using System.Xml;
using System.IO;
using BobDust.Core.Extensions;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	class XmlCommandResult : CommandResult, ICommandResult
	{
		private class XmlNames
		{
			public const string Return = "Return";
			public const string Exception = "Exception";
			public const string Type = "type";
		}

		public XmlCommandResult()
		   : base()
		{
		}

		public XmlCommandResult(string operationName)
		   : base(operationName)
		{
		}

		public XmlCommandResult(string operationName, object returnValue)
		   : base(operationName, returnValue)
		{
		}

		public XmlCommandResult(string operationName, Exception exception)
		   : base(operationName, exception)
		{
		}

		public override void Write(System.IO.BinaryWriter writer)
		{
			var builder = new StringBuilder();
			var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
			using (var xmlWriter = XmlWriter.Create(builder, settings))
			{
				xmlWriter.WriteStartElement(OperationName);
				if (ReturnValue != null)
				{
					xmlWriter.WriteStartElement(XmlNames.Return);
					xmlWriter.Write(ReturnValue);
					xmlWriter.WriteEndElement();
				}
				else if (Exception != null)
				{
					xmlWriter.WriteStartElement(XmlNames.Exception);
					xmlWriter.WriteAttributeString(XmlNames.Type, Exception.GetType().AssemblyQualifiedName);
					xmlWriter.WriteBinary(Exception);
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
					xmlReader.Read();
					var typeAttribute = xmlReader.GetAttribute(XmlNames.Type);
					if (typeAttribute != null)
					{
						var type = Type.GetType(typeAttribute);
						if (xmlReader.Name == XmlNames.Return)
						{
							if (xmlReader.IsStartElement(XmlNames.Return))
							{
								xmlReader.Read();
							}
							ReturnValue = xmlReader.Read(type);
						}
						else if (xmlReader.Name == XmlNames.Exception)
						{
							if (xmlReader.IsStartElement(XmlNames.Exception))
							{
								xmlReader.Read();
							}
							Exception = (Exception)xmlReader.ReadBinary();
						}
					}
				}
			}
		}

	}
}
