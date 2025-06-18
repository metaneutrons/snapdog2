using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;
using SnapDog2.Core.Events;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Infrastructure.Repositories;
using SnapDog2.Server.Features.AudioStreams.Commands;

namespace SnapDog2.Server.Features.AudioStreams.Handlers;

/// <summary>
/// Handler for creating new audio streams in the system.
/// Validates business rules, persists the stream, and publishes domain events.
/// </summary>
public sealed class CreateAudioStreamHandler : IRequestHandler<CreateAudioStreamCommand, Result<AudioStream>>
{
    private readonly IAudioStreamRepository _audioStreamRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateAudioStreamHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAudioStreamHandler"/> class.
    /// </summary>
    /// <param name="audioStreamRepository">The audio stream repository.</param>
    /// <param name="eventPublisher">The event publisher.</param>
    /// <param name="logger">The logger.</param>
    public CreateAudioStreamHandler(
        IAudioStreamRepository audioStreamRepository,
        IEventPublisher eventPublisher,
        ILogger<CreateAudioStreamHandler> logger
    )
    {
        _audioStreamRepository =
            audioStreamRepository ?? throw new ArgumentNullException(nameof(audioStreamRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the creation of a new audio stream.
    /// </summary>
    /// <param name="request">The create audio stream command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created audio stream or an error.</returns>
    public async Task<Result<AudioStream>> Handle(CreateAudioStreamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating new audio stream: {StreamName} with URL: {StreamUrl} and codec: {Codec}",
                request.Name,
                request.Url,
                request.Codec
            );

            // Validate business rules
            var validationResult = await ValidateBusinessRulesAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Business rule validation failed for stream creation: {StreamName}. Error: {Error}",
                    request.Name,
                    validationResult.Error
                );
                return Result<AudioStream>.Failure(validationResult.Error!);
            }

            // Create the audio stream entity
            var streamId = GenerateStreamId();
            var audioStream = CreateAudioStreamEntity(request, streamId);

            // Persist the stream
            var createdStream = await _audioStreamRepository.AddAsync(audioStream, cancellationToken);

            _logger.LogInformation(
                "Successfully created audio stream: {StreamId} - {StreamName}",
                createdStream.Id,
                createdStream.Name
            );

            // Publish domain event
            await PublishAudioStreamCreatedEventAsync(createdStream, request.RequestedBy, cancellationToken);

            return Result<AudioStream>.Success(createdStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create audio stream: {StreamName} with URL: {StreamUrl}",
                request.Name,
                request.Url
            );

            return Result<AudioStream>.Failure($"Failed to create audio stream: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates business rules for creating an audio stream.
    /// </summary>
    /// <param name="request">The create audio stream command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether validation passed.</returns>
    private async Task<Result> ValidateBusinessRulesAsync(
        CreateAudioStreamCommand request,
        CancellationToken cancellationToken
    )
    {
        var errors = new List<string>();

        // Check for unique stream name
        var allStreams = await _audioStreamRepository.GetAllAsync(cancellationToken);
        var existingStreamByName = allStreams.FirstOrDefault(s => s.Name == request.Name);

        if (existingStreamByName != null)
        {
            errors.Add($"A stream with the name '{request.Name}' already exists.");
        }

        // Check for unique URL
        var existingStreamByUrl = await _audioStreamRepository.GetStreamByUrlAsync(request.Url, cancellationToken);
        if (existingStreamByUrl != null)
        {
            errors.Add($"A stream with the URL '{request.Url}' already exists.");
        }

        // Validate sample rate for codec compatibility
        if (!IsValidSampleRateForCodec(request.Codec, request.SampleRate))
        {
            errors.Add($"Sample rate {request.SampleRate} Hz is not supported for codec {request.Codec}.");
        }

        // Validate bitrate if specified
        if (request.BitrateKbps.HasValue && !IsValidBitrate(request.BitrateKbps.Value))
        {
            errors.Add($"Bitrate {request.BitrateKbps} kbps is not within valid range (8-320 kbps).");
        }

        // Validate channels if specified
        if (request.Channels.HasValue && !IsValidChannelCount(request.Channels.Value))
        {
            errors.Add($"Channel count {request.Channels} is not supported (supported: 1-8 channels).");
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>
    /// Creates an audio stream entity from the command.
    /// </summary>
    /// <param name="request">The create audio stream command.</param>
    /// <param name="streamId">The generated stream ID.</param>
    /// <returns>The created audio stream entity.</returns>
    private static AudioStream CreateAudioStreamEntity(CreateAudioStreamCommand request, string streamId)
    {
        var defaultBitrate = GetDefaultBitrateForCodec(request.Codec);
        var defaultChannels = 2; // Default to stereo

        return new AudioStream
        {
            Id = streamId,
            Name = request.Name,
            Url = request.Url,
            Codec = request.Codec,
            BitrateKbps = request.BitrateKbps ?? defaultBitrate,
            Status = StreamStatus.Stopped,
            SampleRateHz = request.SampleRate,
            Channels = request.Channels ?? defaultChannels,
            Description = request.Description,
            Tags = request.Tags,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Publishes the AudioStreamCreated domain event.
    /// </summary>
    /// <param name="audioStream">The created audio stream.</param>
    /// <param name="requestedBy">The user who requested the creation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task PublishAudioStreamCreatedEventAsync(
        AudioStream audioStream,
        string requestedBy,
        CancellationToken cancellationToken
    )
    {
        var audioStreamCreatedEvent = new AudioStreamCreatedEvent
        {
            StreamId = audioStream.Id,
            StreamName = audioStream.Name,
            StreamUrl = audioStream.Url.ToString() ?? string.Empty,
            Codec = audioStream.Codec,
            SampleRate = audioStream.SampleRateHz ?? 0,
            RequestedBy = requestedBy,
        };

        await _eventPublisher.PublishAsync(audioStreamCreatedEvent, cancellationToken);

        _logger.LogDebug("Published AudioStreamCreatedEvent for stream: {StreamId}", audioStream.Id);
    }

    /// <summary>
    /// Generates a unique stream ID.
    /// </summary>
    /// <returns>A unique stream identifier.</returns>
    private static string GenerateStreamId()
    {
        return $"stream_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Validates if the sample rate is compatible with the specified codec.
    /// </summary>
    /// <param name="codec">The audio codec.</param>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <returns>True if compatible; otherwise, false.</returns>
    private static bool IsValidSampleRateForCodec(AudioCodec codec, int sampleRate)
    {
        var validSampleRates = codec switch
        {
            AudioCodec.PCM => new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 },
            AudioCodec.FLAC => new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000 },
            AudioCodec.MP3 => new[] { 8000, 11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000 },
            AudioCodec.AAC => new[]
            {
                8000,
                11025,
                12000,
                16000,
                22050,
                24000,
                32000,
                44100,
                48000,
                64000,
                88200,
                96000,
            },
            AudioCodec.OGG => new[] { 8000, 11025, 16000, 22050, 32000, 44100, 48000 },
            _ => Array.Empty<int>(),
        };

        return validSampleRates.Contains(sampleRate);
    }

    /// <summary>
    /// Validates if the bitrate is within acceptable range.
    /// </summary>
    /// <param name="bitrateKbps">The bitrate in kbps.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidBitrate(int bitrateKbps)
    {
        return bitrateKbps >= 8 && bitrateKbps <= 320;
    }

    /// <summary>
    /// Validates if the channel count is supported.
    /// </summary>
    /// <param name="channels">The number of channels.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidChannelCount(int channels)
    {
        return channels >= 1 && channels <= 8;
    }

    /// <summary>
    /// Gets the default bitrate for a specific codec.
    /// </summary>
    /// <param name="codec">The audio codec.</param>
    /// <returns>The default bitrate in kbps.</returns>
    private static int GetDefaultBitrateForCodec(AudioCodec codec)
    {
        return codec switch
        {
            AudioCodec.PCM => 1411, // CD quality uncompressed
            AudioCodec.FLAC => 1000, // Lossless, variable bitrate
            AudioCodec.MP3 => 128,
            AudioCodec.AAC => 128,
            AudioCodec.OGG => 128,
            _ => 128,
        };
    }
}

/// <summary>
/// Domain event published when an audio stream is created.
/// </summary>
public sealed record AudioStreamCreatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the created audio stream.
    /// </summary>
    public required string StreamId { get; init; }

    /// <summary>
    /// Gets the name of the created audio stream.
    /// </summary>
    public required string StreamName { get; init; }

    /// <summary>
    /// Gets the URL of the created audio stream.
    /// </summary>
    public required string StreamUrl { get; init; }

    /// <summary>
    /// Gets the codec of the created audio stream.
    /// </summary>
    public required AudioCodec Codec { get; init; }

    /// <summary>
    /// Gets the sample rate of the created audio stream.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Gets the user or system that requested the stream creation.
    /// </summary>
    public required string RequestedBy { get; init; }
}
