namespace Simulab.SharedKernel.Security;

/// <summary>Who is acting. The host fills it from the signed-in user (F-5); until then it is anonymous.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
