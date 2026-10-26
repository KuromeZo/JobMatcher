using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobFetcherService
{
    Task<List<JobOffer>> FetchOffersMetadataAsync(IReadOnlySet<string> knownGuids, JobSearchFilters filters, CancellationToken ct);

    /// <summary>Returns null when the body could not be fetched.</summary>
    Task<string?> FetchOfferBodyAsync(string slug, CancellationToken ct);
}
