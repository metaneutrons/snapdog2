using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace SnapDog2.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly MockRepository MockRepository;

    protected TestBase()
    {
        MockRepository = new MockRepository(MockBehavior.Strict);
    }

    protected Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return MockRepository.Create<ILogger<T>>();
    }

    protected Mock<IMediator> CreateMediatorMock()
    {
        return MockRepository.Create<IMediator>();
    }

    public void Dispose()
    {
        MockRepository.Verify();
        GC.SuppressFinalize(this);
    }
}
