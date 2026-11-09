namespace FarmerSample.Core.SharedKernel;

/// <summary>A business rule was violated; the message is safe to show to staff.</summary>
public sealed class DomainException(string message) : Exception(message);
