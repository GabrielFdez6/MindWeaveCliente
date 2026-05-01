using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindWeaveCliente.Services.Abstractions
{
    internal interface IAuthenticationService
    {
        Task<OperationResultDto> registerAsync(UserProfileDto profile, string password);

    }
}
