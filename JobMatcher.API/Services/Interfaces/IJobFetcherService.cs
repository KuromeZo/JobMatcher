using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobFetcherService
{
    Task<List<JobOffer>> FetchJuniorOffersAsync();
}