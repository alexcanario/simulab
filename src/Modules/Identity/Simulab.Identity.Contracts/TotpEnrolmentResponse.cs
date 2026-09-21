namespace Simulab.Identity.Contracts;

/// <summary>
/// A started enrolment (F-11 UC1, BR2): the secret for manual entry, the <c>otpauth://</c> URI and the same URI
/// as a QR code image (a PNG data URI). Two-factor is still off.
/// </summary>
public sealed record TotpEnrolmentResponse(string Secret, string OtpAuthUri, string QrCodeDataUri);
