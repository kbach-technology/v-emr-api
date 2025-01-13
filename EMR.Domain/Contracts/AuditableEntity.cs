using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMR.Domain.Contracts;

public abstract class AuditableEntity<TId> : IAuditableEntity<TId>
{
    public TId Id { get; set; }

    [Column(TypeName = "varchar(225)")] public string CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    [Column(TypeName = "varchar(225)")] public string LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }
}