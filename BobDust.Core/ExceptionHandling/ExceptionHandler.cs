using System;

namespace BobDust.Core.ExceptionHandling
{
    public abstract class ExceptionHandler : IExceptionHandler
   {
      public Action<System.Exception, IExceptionHandler> OnException { get; set; }

      protected virtual void Handle(Exception ex, IExceptionHandler source)
      {
         if (OnException != null)
         {
            OnException(ex, source);
         }
         else
         {
            throw (ex);
         }
      }
   }
}
