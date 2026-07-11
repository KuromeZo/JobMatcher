using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobFetcherService
{
    Task<List<JobOffer>> FetchOffersMetadataAsync(HashSet<string> existingGuids, JobSearchFilters filters);
    Task<string?> FetchOfferBodyAsync(string slug);
}