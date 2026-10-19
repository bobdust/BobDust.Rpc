using System.Formats.Nrbf;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

#if !NET48
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	class NrbfCommandResult : NrbfCommandBase, ICommandResult
	{
		public static NrbfCommandResult NewException(string contractType, string operationName, JsonSerializable exception)
		{
			return new NrbfCommandResult(contractType, operationName)
			{
				SerializedException = exception
			};
		}

		public NrbfCommandResult() : base() { }

		public NrbfCommandResult(string contractType, string operationName) : base(contractType, operationName)
		{
		}

		public NrbfCommandResult(string contractType, string operationName, object? returnValue) : this(contractType, operationName)
		{
			ReturnValue = returnValue;
		}

		public NrbfCommandResult(string contractType, string operationName, Exception exception) : this(contractType, operationName)
		{
			Exception = exception;
		}

		public object? ReturnValue { get; private set; }

		public Exception? Exception { get; private set; }

		public JsonSerializable? SerializedException { get; private set; }

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var commandResult = (NrbfCommandResult)deserialized;
			ReturnValue = commandResult.ReturnValue;
			Exception = commandResult.Exception;
		}

#if !NET48
		private JsonSerializerOptions ExceptionSerializationOptions() => new JsonSerializerOptions
		{
            Converters = { new SafeExceptionConverter() },
			WriteIndented = true
		};
#else
		private JsonSerializerSettings ExceptionSerializationOptions() => new JsonSerializerSettings
		{
			// Crucial for .NET 4.8 Exceptions to prevent looping errors
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			Formatting = Formatting.Indented,
			Converters = { new SafeExceptionConverter() }
		};
#endif

		protected override NrbfCommandBase Deserialize(string contractType, string operationName, ClassRecord rootRecord)
		{
			var typeProp = BackingField(nameof(JsonSerializable.Type));
			var bytesProp = BackingField(nameof(JsonSerializable.Bytes));
			var exceptionRecord = rootRecord.GetClassRecord(BackingField(nameof(SerializedException)));
			if (exceptionRecord is null)
			{
				var valueRecord = rootRecord.GetClassRecord(BackingField(nameof(ReturnValue)));
				if (valueRecord is null)
				{
					return new NrbfCommandResult(contractType, operationName, default(object?));
				}
				var typeName = valueRecord!.GetString(typeProp);
				var type = Type.GetType(typeName!);
				var bytes = ((SZArrayRecord<byte>)valueRecord!.GetArrayRecord(bytesProp)!).GetArray();
#if !NET48
				var returnValue = JsonSerializer.Deserialize(bytes, type!);
#else
				var returnValue = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type!);
#endif
				return new NrbfCommandResult(contractType, operationName, returnValue);
			}

			var exceptionTypeName = exceptionRecord.GetString(typeProp);
			var exceptionType = Type.GetType(exceptionTypeName!);
			var exceptionBytes = ((SZArrayRecord<byte>)exceptionRecord.GetArrayRecord(bytesProp)!).GetArray();
#if !NET48
			Exception exception = (Exception)JsonSerializer.Deserialize(exceptionBytes, exceptionType!, ExceptionSerializationOptions())!;
#else
			var exception = (Exception)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(exceptionBytes), exceptionType, ExceptionSerializationOptions())!;
#endif
			return new NrbfCommandResult(contractType, operationName, exception);
		}

#pragma warning disable SYSLIB0011 // Type or member is obsolete
		protected override void Serialize(BinaryFormatter formatter, Stream stream)
#pragma warning restore SYSLIB0011 // Type or member is obsolete
		{
			var valueType = ReturnValue?.GetType();
			var exceptionType = Exception?.GetType();
			object serializing = Exception is null
				? new NrbfCommandResult(
					ContractType!,
					OperationName!,
#if !NET48
					valueType is null ? null : new JsonSerializable(valueType.AssemblyQualifiedName!, JsonSerializer.SerializeToUtf8Bytes(ReturnValue, valueType)))
#else
					valueType is null ? null : new JsonSerializable(valueType.AssemblyQualifiedName!, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ReturnValue, valueType, null))))
#endif
				: NewException(
					ContractType!,
					OperationName!,
#if !NET48
					new JsonSerializable(exceptionType!.AssemblyQualifiedName!, JsonSerializer.SerializeToUtf8Bytes(Exception, exceptionType, ExceptionSerializationOptions())));
#else
					new JsonSerializable(exceptionType!.AssemblyQualifiedName!, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Exception, exceptionType, ExceptionSerializationOptions()))));
#endif
			formatter.Serialize(stream, serializing);
		}
	}
}
