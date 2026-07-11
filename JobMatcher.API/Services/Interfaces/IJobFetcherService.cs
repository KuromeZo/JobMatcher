using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobFetcherService
{
    Task<List<JobOffer>> FetchOffersMetadataAsync(HashSet<string> existingGuids);
    Task<string?> FetchOfferBodyAsync(string slug);
}