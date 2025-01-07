using Sufficit.Identity;

namespace Sufficit.Net.Http
{
    public interface IAuthenticatedControllerBase : IControllerBase
    {
        ITokenProvider Tokens { get; }
    }
}
