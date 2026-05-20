using MindWeaveClient.ProfileService;
using MindWeaveClient.Services.Abstractions;
using System;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        public async Task<PlayerProfileViewDto> getPlayerProfileViewAsync(string username)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.getPlayerProfileViewAsync(username));
        }

        public async Task<UserProfileForEditDto> getPlayerProfileForEditAsync(string username)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.getPlayerProfileForEditAsync(username));
        }

        public async Task<AchievementDto[]> getPlayerAchievementsAsync(int playerId)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.getPlayerAchievementsAsync(playerId));
        }

        public async Task<OperationResultDto> updateProfileAsync(string username, UserProfileForEditDto updatedProfile)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.updateProfileAsync(username, updatedProfile));
        }

        public async Task<OperationResultDto> changePasswordAsync(string username, string currentPassword, string newPassword)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.changePasswordAsync(username, currentPassword, newPassword));
        }

        public async Task<OperationResultDto> updateAvatarPathAsync(string username, string avatarPath)
        {
            return await executeServiceCallAsync(async (client) =>
                await client.updateAvatarPathAsync(username, avatarPath));
        }

        private static async Task<T> executeServiceCallAsync<T>(Func<ProfileManagerClient, Task<T>> action)
        {
            var client = new ProfileManagerClient();

            try
            {
                T result = await action(client);
                closeClientSafe(client);
                return result;
            }
            catch (EndpointNotFoundException)
            {
                abortClientSafe(client);
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                abortClientSafe(client);
                throw;
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

        private static void closeClientSafe(ProfileManagerClient client)
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

        private static void abortClientSafe(ProfileManagerClient client)
        {
            try
            {
                client.Abort();
            }
            catch
            {
                /*
                 *Ignore: The goal is to try to release the resource.
                 * If the proxy is already in a faulted or disconnected state,
                 * Abort() might throw an exception that adds no value
                 * and could interrupt the application's shutdown flow.
               */
            }
        }

    }
}