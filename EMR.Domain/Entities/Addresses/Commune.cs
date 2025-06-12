using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Addresses;

public class Commune : AuditableEntity<Guid>
{
    [Required] public Guid DistrictId { get; set; }

    [ForeignKey(nameof(DistrictId))] public District District { get; set; }

    [Required] [MaxLength(10)] public string CommuneCode { get; set; }

    [Required] [MaxLength(100)] public string CommuneNameEn { get; set; }

    [Required] [MaxLength(100)] public string CommuneNameKm { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Village> Villages { get; set; }
}