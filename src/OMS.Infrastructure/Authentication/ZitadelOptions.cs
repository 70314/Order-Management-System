namespace OMS.Infrastructure.Authentication;

public class ZitadelOptions {
    public const string SectionName = "Authentication:Zitadel";

    /// <summary>The Zitadel instance URL (e.g., https://your-instance.zitadel.cloud)</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>The audience (project ID or resource server client ID) for token validation</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Whether to validate the audience claim in the token</summary>
    public bool ValidateAudience { get; set; } = true;
}
