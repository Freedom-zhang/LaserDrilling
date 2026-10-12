using System;
using System.Threading;
using System.Threading.Tasks;

namespace LaserDrilling.Hmi.Services
{
    /// <summary>
    /// 保留历史教学用认证接口；实际认证使用账户服务。
    /// </summary>
    public interface IDemoAuthenticationService
    {
        Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken);
    }
    /// <summary>
    /// 保留历史教学用模拟认证实现，当前应用未注册或使用此服务。
    /// </summary>
    public sealed class DemoAuthenticationService : IDemoAuthenticationService
    {
        public async Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken)
        {
            await Task.Delay(250, cancellationToken);
            return string.Equals(userName, "operator", StringComparison.OrdinalIgnoreCase) && password == "123456";
        }
    }
}
