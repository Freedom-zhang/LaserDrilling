using System;
using System.Threading;
using System.Threading.Tasks;

namespace LaserDrilling.Hmi.Services
{
    // Historical teaching example only. No longer registered or used by the application.
    // Active authentication and authorization live in IAccountService / LocalAccountService.
    public interface IDemoAuthenticationService
    {
        Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken);
    }
    public sealed class DemoAuthenticationService : IDemoAuthenticationService
    {
        public async Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken)
        {
            await Task.Delay(250, cancellationToken);
            return string.Equals(userName, "operator", StringComparison.OrdinalIgnoreCase) && password == "123456";
        }
    }
}
