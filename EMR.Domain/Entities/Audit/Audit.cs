using System;
using System.ComponentModel.DataAnnotations;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Audit;

public class Audit : IEntity<string>
{
    public string UserId { get; set; }
    public string Type { get; set; }
    public string TableName { get; set; }
    public DateTime DateTime { get; set; }
    public string OldValues { get; set; }
    public string NewValues { get; set; }
    public string AffectedColumns { get; set; }
    public string PrimaryKey { get; set; }

    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
}