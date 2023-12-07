using System;

namespace BobDust.Core.ExceptionHandling
{
    public interface IExceptionHandler
   {
      Action<Exception, IExceptionHandler> OnException { get; set; }
   }
}
