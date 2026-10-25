using ReleaseDeck.Core;
using System.Text.Json;

namespace ReleaseDeck.Portal.Services;

/// <summary>Only shared account fields needed by the release UI; entitlements remain authoritative at the service.</summary>
public sealed class CreatorAccount { public string Id { get; set; } = ""; public bool Premium { get; set; } public int ProjectLimit { get; set; } public int UsedProjects { get; set; } }
public sealed class AccountStatus { public CreatorAccount? Account { get; set; } }
public sealed class ServiceConfiguration { public bool Configured { get; set; } public bool GoogleConfigured { get; set; } public CreatorAccount? Account { get; set; } public JsonElement Limits { get; set; } }
public sealed class WorkspaceList { public List<Workspace> Workspaces { get; set; } = []; public CreatorAccount? Account { get; set; } }
public sealed class WorkspaceEnvelope { public Workspace Workspace { get; set; } = new(); }
/// <summary>The editable document and current access decision returned by the workspace API.</summary>
public sealed class Workspace { public string Id { get; set; } = ""; public string Title { get; set; } = ""; public int Revision { get; set; } public string Role { get; set; } = "viewer"; public long? ArchivedAt { get; set; } public long UpdatedAt { get; set; } public ReleaseProject Body { get; set; } = new(); public int? ApprovedRevision { get; set; } public bool CanOperate { get; set; } }
public sealed class MemberList { public List<WorkspaceMember> Members { get; set; } = []; }
public sealed class WorkspaceMember { public string Id { get; set; } = ""; public string MemberId { get; set; } = ""; public string Role { get; set; } = ""; }
public sealed class ReviewList { public List<ReleaseReview> Reviews { get; set; } = []; }
public sealed class ReleaseReview { public string Id { get; set; } = ""; public string MemberId { get; set; } = ""; public int Revision { get; set; } public string Decision { get; set; } = ""; public string Note { get; set; } = ""; public long CreatedAt { get; set; } }
public sealed class Invitation { public string Token { get; set; } = ""; public long ExpiresAt { get; set; } }
public sealed class InvitationList { public List<InvitationSummary> Invites { get; set; } = []; }
public sealed class InvitationSummary { public string Id { get; set; } = ""; public string Role { get; set; } = "viewer"; public long ExpiresAt { get; set; } public long? RevokedAt { get; set; } public bool Used { get; set; } }
public sealed class JoinedWorkspace { public string WorkspaceId { get; set; } = ""; }
public sealed class ConnectionList { public List<ChannelConnection> Connections { get; set; } = []; }
public sealed class ChannelConnection { public string Id { get; set; } = ""; public string ChannelId { get; set; } = ""; public string Title { get; set; } = ""; public long ExpiresAt { get; set; } public long UpdatedAt { get; set; } public List<string> Scopes { get; set; } = []; }
public sealed class AuthorizationDestination { public string AuthorizationUrl { get; set; } = ""; }
public sealed class DisconnectionResult { public bool Disconnected { get; set; } public bool ProviderRevoked { get; set; } }
public sealed class OperationList { public List<RemoteOperation> Operations { get; set; } = []; }
public sealed class OperationEnvelope { public RemoteOperation Operation { get; set; } = new(); }
/// <summary>A server-owned remote receipt; no upload capability or provider session URL is accepted into this type.</summary>
public sealed class RemoteOperation { public string Id { get; set; } = ""; public string WorkspaceId { get; set; } = ""; public string EpisodeId { get; set; } = ""; public int Revision { get; set; } public int? AppliedRevision { get; set; } public JsonElement FileIdentity { get; set; } public string Status { get; set; } = ""; public string? VideoId { get; set; } public long Offset { get; set; } public long TotalBytes { get; set; } public string? Processing { get; set; } public string Action { get; set; } = ""; public bool Uncertain { get; set; } public string? Error { get; set; } public long CreatedAt { get; set; } public long UpdatedAt { get; set; } }
public sealed class ServiceFailure { public string Error { get; set; } = ""; public string Code { get; set; } = ""; }
public sealed record EmptyAcknowledgement(bool Ok = true);
