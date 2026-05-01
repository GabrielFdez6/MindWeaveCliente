using MindWeaveCliente.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MindWeaveCliente.Services.Implementations
{
    internal class AuthenticationService : IAuthenticationService
    {
        public async Task<OperationResultDto> registerAsync(UserProfileDto profile, string password)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.registerAsync(profile, password));
        }

        private static async Task<T> executeServiceCallAsync<T>(Func<AuthenticationManagerClient, Task<T>> serviceCall)
        {
            var client = new AuthenticationManagerClient();

            try
            {
                T result = await serviceCall(client);
                closeClientSafe(client);
                return result;
            }
            catch (CommunicationException)
            {
                abortClientSafe(client);
                throw;
            }
            catch (TimeoutException)
            {
                abortClientSafe(client);
                throw;
            }
            catch (SocketException)
            {
                abortClientSafe(client);
                throw;
            }

        }

        private static void closeClientSafe(AuthenticationManagerClient client)
        {
            try
            {
                if (client.State == CommunicationState.Opened)
                {
                    client.Close();
                }
            }
            catch (CommunicationException)
            {
                client.Abort();
            }
            catch (TimeoutException)
            {
                client.Abort();
            }
        }

    }
