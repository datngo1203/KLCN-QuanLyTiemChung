namespace TiemChung.Domain.Entities;

public sealed class IdentitySession
{
    public Guid Id { get; set; }
    public int AccountId { get; set; }
    public long SecurityVersion { get; set; }
    public string ClientType { get; set; } = "native";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public byte[]? CsrfSecretHash { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RefreshCredential
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public Guid? ReplacedById { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class EmailVerification
{
    public Guid Id { get; set; }
    public int AccountId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
}

public sealed class IdentityMailOutbox
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public byte[]? EncryptedPayload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastErrorCode { get; set; }
}

public sealed class AccountProfileLink
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int? CustomerId { get; set; }
    public int? RecipientId { get; set; }
    public string State { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SecurityAudit
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int? ActorAccountId { get; set; }
    public int? SubjectAccountId { get; set; }
    public Guid? SessionId { get; set; }
    public string Action { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string TraceId { get; set; } = "";
    public string? ChangedFields { get; set; }
    public Guid? OperationId { get; set; }
    public string? OperatorIdentity { get; set; }
}

public sealed class IdentityRecoveryDatabase
{
    public int Id { get; set; }
    public Guid RecoveryDatabaseId { get; set; }
    public string M1UpgradeEpoch { get; set; } = "M1";
}

public sealed class IdentityRecoveryOperation
{
    public Guid OperationId { get; set; }
    public Guid RecoveryDatabaseId { get; set; }
    public string M1UpgradeEpoch { get; set; } = "M1";
    public byte[] ManifestFingerprint { get; set; } = [];
    public int TargetAccountId { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public string OperatorIdentity { get; set; } = "";
    public string ResultCode { get; set; } = "";
}
