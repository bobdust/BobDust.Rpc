using System.Xml;
using BobDust.Core.Extensions;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	class XmlCommand : Command, ICommand
	{
		public string? ContractType { get; private set; }

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

		public XmlCommand(string contractType, string operationName, IEnumerable<(string Name, object Value)> parameters)
		   : this(operationName, parameters)
		{
			ContractType = contractType;
		}

		public override ICommandResult Return()
		{
			return new XmlCommandResult(OperationName!);
		}

		public override ICommandResult Return(object? value)
		{
			return new XmlCommandResult(OperationName!, value);
		}

		public override ICommandResult Throw(Exception exception)
		{
			return new XmlCommandResult(OperationName!, exception);
		}

		public override void Write(Stream stream)
		{
			var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
			using var xmlWriter = XmlWriter.Create(stream, settings);
			xmlWriter.WriteStartElement(ContractType!);
			xmlWriter.WriteStartElement(OperationName!);
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
			xmlWriter.WriteEndElement();
		}

		public override void Read(Stream stream)
		{
			using var xmlReader = XmlReader.Create(stream);
			xmlReader.Read();
			ContractType = xmlReader.Name;

			xmlReader.Read();
			OperationName = xmlReader.Name;
			var paramList = new List<(string Name, object Value)>();
			while (xmlReader.ReadToFollowing(XmlNames.Parameter))
			{
				var name = xmlReader.GetAttribute(XmlNames.Name);
				var type = Type.GetType(xmlReader.GetAttribute(XmlNames.Type)!);
				xmlReader.Read();
				var obj = xmlReader.Read(type!);
				paramList.Add((name!, obj));
			}
			Parameters = paramList;
		}
	}
}
