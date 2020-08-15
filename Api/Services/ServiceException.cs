using System;

namespace Api.Services
{
    public class ServiceException : ApplicationException
    {
        /// <inheritdoc />
        public ServiceException()
        {
        }

        /// <inheritdoc />
        public ServiceException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public ServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
