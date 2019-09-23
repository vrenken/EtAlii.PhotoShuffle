#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Threading.Tasks;

    public static class TaskExtensions
    {
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler handler = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                handler?.HandleError(ex);
            }
        }
    }
}
