namespace EtAlii.PhotoShuffle
{
    using System;

    public interface IErrorHandler
    {
        void HandleError(Exception ex);
    }
}
