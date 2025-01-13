using System.ComponentModel.DataAnnotations;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Settings;

public class Preference : AuditableEntity<string>
{
    [Required] public string UserId { get; set; }

    public string LanguageCode { get; set; }
}