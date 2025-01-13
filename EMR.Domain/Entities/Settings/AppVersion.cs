using System;
using System.ComponentModel.DataAnnotations;
using EMR.Domain.Contracts;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities.Settings;

public class AppVersion : AuditableEntity<string>
{
    [Required] public Platform Platform { get; set; }

    [Required] public string VersionNumber { get; set; }

    [Required] public int BuildNumber { get; set; }

    public string UpdateMessage { get; set; }

    [Required] public bool IsForceUpdate { get; set; }

    public DateTime ReleaseDate { get; set; }
}