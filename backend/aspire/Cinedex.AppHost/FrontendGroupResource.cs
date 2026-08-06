namespace Cinedex.AppHost;

/// <summary>
/// A dashboard-only parent for the two frontend dev servers, so they appear as one expandable
/// <c>frontend</c> node instead of two resources scattered among the .NET hosts and containers.
/// </summary>
/// <param name="name">The resource name, shown as the node's label in the dashboard.</param>
/// <remarks>
/// <para>
/// Aspire has no built-in "logical group" resource: <c>WithParentRelationship</c> nests one resource
/// under another, but the parent has to be a real resource, and neither the SPA nor Storybook should
/// own the other. This type exists to be that parent and nothing else — it starts no process, has no
/// endpoint, and has no health check.
/// </para>
/// <para>
/// Because it never runs, nothing would publish a state for it and the dashboard would leave the row
/// blank; <c>AddCinedexFrontendGroup</c> supplies one via <c>WithInitialState</c> instead. The
/// relationship is presentational only — Aspire does not derive startup ordering from it, so the two
/// children keep their own <c>WaitFor</c> wiring (the SPA waits on the web service, Storybook on
/// nothing).
/// </para>
/// </remarks>
internal sealed class FrontendGroupResource(string name) : Resource(name);
