using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Addresses;

public class Province : AuditableEntity<Guid>
{
    [Required] public Guid CountryId { get; set; }

    [Required] [MaxLength(10)] public string ProvinceCode { get; set; }

    [Required] [MaxLength(100)] public string ProvinceNameEn { get; set; }

    [Required] [MaxLength(100)] public string ProvinceNameKm { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<District> Districts { get; set; }
}