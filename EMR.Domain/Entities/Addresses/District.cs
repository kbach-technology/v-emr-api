using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Addresses;

public class District : AuditableEntity<Guid>
{
    [Required] public Guid ProvinceId { get; set; }

    [ForeignKey(nameof(ProvinceId))] public Province Province { get; set; }

    [Required] [MaxLength(10)] public string DistrictCode { get; set; }

    [Required] [MaxLength(100)] public string DistrictNameEn { get; set; }

    [Required] [MaxLength(100)] public string DistrictNameKm { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Commune> Communes { get; set; }
}