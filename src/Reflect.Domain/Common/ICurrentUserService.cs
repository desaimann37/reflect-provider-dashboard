namespace Reflect.Domain.Common;

public interface ICurrentUserService
{
    Guid? UserId { get; }          // the local user id (not the Entra object id)
    string EntraObjectId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
