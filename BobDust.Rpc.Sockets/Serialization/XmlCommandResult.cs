using System.Xml;
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

		public XmlCommandResult(string operationName, object? returnValue)
		   : base(operationName, returnValue)
		{
		}

		public XmlCommandResult(string operationName, Exception exception)
		   : base(operationName, exception)
		{
		}

		public override void Write(Stream stream)
		{
			var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
			using var xmlWriter = XmlWriter.Create(stream, settings);
			xmlWriter.WriteStartElement(OperationName!);
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
				xmlWriter.WriteObject(Exception);
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
		}

		public override void Read(Stream stream)
		{
			using var xmlReader = XmlReader.Create(stream);
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
					ReturnValue = xmlReader.Read(type!);
				}
				else if (xmlReader.Name == XmlNames.Exception)
				{
					if (xmlReader.IsStartElement(XmlNames.Exception))
					{
						xmlReader.Read();
					}
					Exception = xmlReader.ReadObject<Exception>();
				}
			}
		}
	}
}
