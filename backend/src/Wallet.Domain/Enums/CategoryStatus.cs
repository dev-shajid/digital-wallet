namespace WalletSystem.Domain.Enums;

/// <summary>An INACTIVE category blocks new expenses but old expenses that used it stay visible.</summary>
public enum CategoryStatus
{
    ACTIVE,
    INACTIVE
}
