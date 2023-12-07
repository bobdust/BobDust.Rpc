using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	public abstract class PipelineDecorator : Pipeline
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

		public override int Read(byte[] buffer)
		{
			return _pipeline.Read(buffer);
		}

	}
}
