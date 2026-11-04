namespace TiemChung.Contracts;

public sealed record VaccinationJourneyDto(int RegistrationId, DateTimeOffset CreatedAt, DateOnly? PreferredDate,
    RegistrationPaymentDto Payment, AppointmentDto[] Appointments, AppointmentDto? NextAppointment,
    VaccinationHistoryDto[] Shots)
{
    public int ShotCount => Shots.Length;
}

public sealed record VaccinationJourneyListResponse(VaccinationJourneyDto[] Items);
