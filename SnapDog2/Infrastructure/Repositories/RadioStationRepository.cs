using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RadioStation entities with domain-specific operations.
/// Provides Entity Framework Core-based data access for radio stations.
/// </summary>
public sealed class RadioStationRepository : RepositoryBase<RadioStation, string>, IRadioStationRepository
{
    /// <summary>
    /// Initializes a new instance of the RadioStationRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public RadioStationRepository(SnapDogDbContext context)
        : base(context) { }

    /// <summary>
    /// Retrieves radio stations by genre.
    /// </summary>
    /// <param name="genre">The genre to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of radio stations in the specified genre.</returns>
    /// <exception cref="ArgumentException">Thrown when genre is null or empty.</exception>
    public async Task<IEnumerable<RadioStation>> GetStationsByGenreAsync(
        string genre,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            throw new ArgumentException("Genre cannot be null or empty.", nameof(genre));
        }

        return await GetQueryableNoTracking()
            .Where(station => station.Genre != null && station.Genre.ToLower().Contains(genre.ToLower()))
            .Where(station => station.IsEnabled)
            .OrderByDescending(station => station.Priority)
            .ThenBy(station => station.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves radio stations by country.
    /// </summary>
    /// <param name="country">The country to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of radio stations from the specified country.</returns>
    /// <exception cref="ArgumentException">Thrown when country is null or empty.</exception>
    public async Task<IEnumerable<RadioStation>> GetStationsByCountryAsync(
        string country,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country cannot be null or empty.", nameof(country));
        }

        return await GetQueryableNoTracking()
            .Where(station => station.Country != null && station.Country.ToLower().Contains(country.ToLower()))
            .Where(station => station.IsEnabled)
            .OrderByDescending(station => station.Priority)
            .ThenBy(station => station.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a radio station by its name.
    /// </summary>
    /// <param name="name">The station name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The radio station if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public async Task<RadioStation?> GetStationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Station name cannot be null or empty.", nameof(name));
        }

        return await GetQueryableNoTracking()
            .FirstOrDefaultAsync(station => station.Name.ToLower() == name.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves radio stations using the specified codec.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of radio stations using the specified codec.</returns>
    public async Task<IEnumerable<RadioStation>> GetStationsByCodecAsync(
        AudioCodec codec,
        CancellationToken cancellationToken = default
    )
    {
        return await GetQueryableNoTracking()
            .Where(station => station.Codec == codec)
            .Where(station => station.IsEnabled)
            .OrderByDescending(station => station.Priority)
            .ThenBy(station => station.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
