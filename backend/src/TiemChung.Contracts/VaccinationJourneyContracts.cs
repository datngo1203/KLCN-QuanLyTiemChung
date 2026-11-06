namespace TiemChung.Contracts;

public sealed record VaccinationJourneyDto(int RegistrationId, DateTimeOffset CreatedAt, DateOnly? PreferredDate,
    RegistrationPaymentDto Payment, AppointmentDto[] Appointments, AppointmentDto? NextAppointment,
    VaccinationHistoryDto[] Shots)
{
    public DateTime? PreferredAt { get; init; }
    public int ShotCount => Shots.Length;
    public VaccinationProgressDto[] Progress { get; init; } = [];
}

public sealed record VaccinationProgressDto(int VaccineId, string VaccineName, int? RegisteredDoses,
    int AdministeredDoses, int? RemainingDoses, AppointmentDto? NextAppointment);

public sealed record VaccinationJourneyListResponse(VaccinationJourneyDto[] Items);
