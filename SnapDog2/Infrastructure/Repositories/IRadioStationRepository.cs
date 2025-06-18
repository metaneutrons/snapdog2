using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Repository interface for RadioStation entities with domain-specific operations.
/// Extends the base repository with radio station-specific query methods.
/// </summary>
public interface IRadioStationRepository : IRepository<RadioStation, string>
{
    /// <summary>
    /// Retrieves radio stations by genre.
    /// </summary>
    /// <param name="genre">The genre to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of radio stations in the specified genre.</returns>
    Task<IEnumerable<RadioStation>> GetStationsByGenreAsync(
        string genre,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves radio stations by country.
    /// </summary>
    /// <param name="country">The country to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of radio stations from the specified country.</returns>
    Task<IEnumerable<RadioStation>> GetStationsByCountryAsync(
        string country,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves a radio station by its name.
    /// </summary>
    /// <param name="name">The station name to search for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The radio station if found; otherwise, null.</returns>
    Task<RadioStation?> GetStationByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves radio stations using the specified codec.
    /// </summary>
    /// <param name="codec">The audio codec to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of radio stations using the specified codec.</returns>
    Task<IEnumerable<RadioStation>> GetStationsByCodecAsync(
        AudioCodec codec,
        CancellationToken cancellationToken = default
    );
}
