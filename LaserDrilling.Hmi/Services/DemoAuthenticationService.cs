using System;
using System.Threading;
using System.Threading.Tasks;

namespace LaserDrilling.Hmi.Services
{
    // Temporary UI demonstration only. Replace with real identity verification later.
    public interface IDemoAuthenticationService
    {
        Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken);
    }
    public sealed class DemoAuthenticationService : IDemoAuthenticationService
    {
        public async Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken)
        {
            await Task.Delay(250, cancellationToken);
            return string.Equals(userName, "operator", StringComparison.OrdinalIgnoreCase) && password == "Demo@123";
        }
    }
}
