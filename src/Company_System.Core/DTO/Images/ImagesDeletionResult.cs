namespace HR_System.Core.DTO.Images;

/// <summary>
/// Outcome of a bulk delete from the image host, as public ids.
/// </summary>
/// <param name="Gone">Deleted now, or already missing from the host. Safe to forget locally.</param>
/// <param name="Failed">Still on the host (or the request failed). Must be retried later.</param>
public record ImagesDeletionResult(IReadOnlyList<string> Gone, IReadOnlyList<string> Failed);
