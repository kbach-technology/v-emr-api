namespace EMR.Domain.Enums;

public enum AdmissionStatus
{
    Active = 1,
    Discharged = 2,
    Transferred = 3,
    LAMA = 4, // Left Against Medical Advice
    Deceased = 5,
    OnHold = 6,
}