using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	abstract class PipelineDecorator : Pipeline
	{
		private IPipeline _pipeline;

		protected PipelineDecorator(IPipeline pipeline)
		{
			_pipeline = pipeline;
		}

		public override void Write(byte[] buffer)
		{
			_pipeline.Write(buffer);
		}

		public override async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken)
		{
			await _pipeline.WriteAsync(buffer, cancellationToken);
		}

		public override async Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken)
		{
			return await _pipeline.ReadAsync(buffer, cancellationToken);
		}

		public override int Read(byte[] buffer)
		{
			return _pipeline.Read(buffer);
		}

	}
}
