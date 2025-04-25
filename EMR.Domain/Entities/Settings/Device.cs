using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMR.Domain.Contracts;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities.Settings;

public class Device : AuditableEntity<string>
{
    [Column(TypeName = "varchar(255)")]
    [Required]
    public string UserId { get; set; }

    [Column(TypeName = "text")] [Required] public string DeviceToken { get; set; }

    public PlatformType PlatformType { get; set; }

    [Column(TypeName = "text")] public string DeviceName { get; set; }

    [Column(TypeName = "text")] public string Manufacturer { get; set; }

    [Column(TypeName = "text")] public string UserAgent { get; set; }

    [Column(TypeName = "text")] public string Model { get; set; }

    [Column(TypeName = "text")] public string SerialNumber { get; set; }
}