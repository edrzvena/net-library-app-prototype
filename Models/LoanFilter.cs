namespace LibraryAppPrototype.Models;

// BR-19: Overdue BUKAN nilai LoanStatus — statusnya turunan, tidak disimpan.
// Enum ini ada supaya UI punya cara menyaring "terlambat" tanpa perlu menambah
// Overdue ke LoanStatus. Terjemahan predikatnya ada di LoanService.SearchAsync (PRD 7.3).
public enum LoanFilter
{
    Active,
    Overdue,
    Returned,
    Lost
}
