using Reflect.Domain.Clinical;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public static class PhiAccessLogFactory
{
    public const string ViewedRecord = "Viewed patient record";
    public const string GeneratedBrief = "Generated pre-session brief";

    public static PhiAccessLog Create(Guid patientId, Guid userId, string userName, string action, string path, DateTimeOffset now) => new()
    {
        PatientId = patientId,
        UserId = userId,
        UserDisplayName = userName,
        Action = action,
        Path = path,
        OccurredAt = now,
        DateCreated = now
    };
}
