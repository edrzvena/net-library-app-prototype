namespace LibraryAppPrototype.Services;

// Angka-angka ini TIDAK BOLEH ditulis ulang sebagai literal di service maupun di .razor.
public static class LoanPolicy
{
    public const int MaxActiveLoansPerMember = 3;      // BR-01
    public const int LoanDurationDays = 7;             // BR-02
    public const int MaxRenewalCount = 1;              // BR-07
    public const int RenewalExtensionDays = 7;         // BR-07
    public const decimal FinePerLateDay = 1_000m;      // BR-06 (IDR)
    public const decimal DamagedBookFineRatio = 0.5m;  // BR-23
}
