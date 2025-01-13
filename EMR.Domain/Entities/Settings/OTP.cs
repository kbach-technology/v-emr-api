using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMR.Domain.Contracts;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities.Settings;

public class OTP : AuditableEntity<string>
{
    [Column(TypeName = "varchar(6)")]
    [Required]
    public string Code { get; set; }

    [Column(TypeName = "varchar(20)")] public string PhoneNumber { get; set; }

    [Column(TypeName = "varchar(255)")] public string Email { get; set; }

    public bool IsValid { get; set; }

    public DateTime ExpiredOn { get; set; }

    public OTPAction Action { get; set; }

    public string IpAddress { get; set; }
}