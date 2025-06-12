using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Addresses;

public class Village : AuditableEntity<Guid>
{
    [Required] public Guid CommuneId { get; set; }

    [ForeignKey(nameof(CommuneId))] public Commune Commune { get; set; }

    [Required] [MaxLength(10)] public string VillageCode { get; set; }

    [Required] [MaxLength(100)] public string VillageNameEn { get; set; }

    [Required] [MaxLength(100)] public string VillageNameKm { get; set; }

    public bool IsActive { get; set; } = true;
}