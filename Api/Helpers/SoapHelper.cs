using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Api.Services;
using Microsoft.Extensions.Logging;

namespace Api.Helpers
{
    public static class SoapHelper {
        /// <summary>
        ///     Wraps SOAP logic related to opening/closing channels and exception handling.
        /// </summary>
        /// <typeparam name="TClient">HTTP Client.</typeparam>
        /// <typeparam name="TResult">Return type of provided delegate.</typeparam>
        /// <param name="client">Pre-configured SOAP Client.</param>
        /// <param name="action">Delegate containing logic to be executed whilst channel is open.</param>
        /// <param name="logger">Logger (optional).</param>
        /// <returns>Return value of the provided delegate.</returns>
        public static async Task<TResult> IssueSoapCallAsync<TClient, TResult>(ClientBase<TClient> client,
            Func<TClient, Task<TResult>> action, ILogger logger = null) where TClient : class{
            var success = false;
            var channel = client.ChannelFactory.CreateChannel();
            logger?.LogInformation($"Client channel opened with {client.Endpoint.Address.Uri}");
            try {
                var result = await action(channel);

                (channel as IClientChannel)?.Close();
                logger?.LogInformation("Client channel connection closed.");
                success = true;

                return result;
            }
            catch (FaultException e) {
                throw new ServiceException(e.Message, e);
            }
            catch (CommunicationException e) {
                throw new ServiceException(e.Message, e);
            }
            catch (TimeoutException e) {
                throw new ServiceException(e.Message, e);
            }
            catch (Exception e) {
                throw new ServiceException(e.Message, e);
            }
            finally {
                if (!success) (channel as IClientChannel)?.Abort();
            }
        }
    }}
