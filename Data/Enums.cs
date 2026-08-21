namespace LibraryAppPrototype.Data;

// `: byte` -> EF Core memetakannya ke TINYINT tanpa perlu converter.
public enum BookCopyStatus : byte { Available = 0, OnLoan = 1, Lost = 2, Damaged = 3, Retired = 4 }
public enum MemberStatus : byte { Active = 0, Suspended = 1, Inactive = 2 }

// BR-19: LoanStatus TIDAK boleh punya nilai Overdue. Keterlambatan dihitung dari DueDate,
// dan untuk penyaringan dipakai LoanFilter (bagian 7.3).
public enum LoanStatus : byte { Active = 0, Returned = 1, Lost = 2 }
public enum FineStatus : byte { Unpaid = 0, Paid = 1, Waived = 2 }
public enum FineReason : byte { LateReturn = 0, LostBook = 1, DamagedBook = 2 }
