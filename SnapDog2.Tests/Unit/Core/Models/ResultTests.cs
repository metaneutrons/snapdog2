//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Tests.Unit.Core.Models;

using FluentAssertions;
using SnapDog2.Core.Models;

public class ResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Failure_WithException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.Failure(exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be(exception.Message);
        result.Exception.Should().Be(exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Failure_WithNullOrEmptyMessage_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Result.Failure(string.Empty));
        Assert.Throws<ArgumentNullException>(() => Result.Failure((string)null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Failure_WithNullException_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Result.Failure((Exception)null!));
    }
}

public class ResultTTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Success_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result<string>.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Value_OnFailedResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result<string>.Failure("Error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void Failure_WithException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result<string>.Failure(exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be(exception.Message);
        result.Exception.Should().Be(exception);
    }
}
