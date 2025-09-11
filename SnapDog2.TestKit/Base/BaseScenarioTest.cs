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
namespace SnapDog2.TestKit.Base;

using System.Text;

/// <summary>
/// Base class for scenario tests providing common HTTP client functionality.
/// </summary>
public abstract class BaseScenarioTest : IScenarioTest
{
    protected readonly HttpClient HttpClient;
    protected readonly string BaseUrl;

    protected BaseScenarioTest(string baseUrl)
    {
        BaseUrl = baseUrl;
        HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>
    /// Gets the display name of the scenario test.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the emoji icon for the scenario test.
    /// </summary>
    public abstract string Icon { get; }

    /// <summary>
    /// Executes the scenario test.
    /// </summary>
    public abstract Task<bool> ExecuteAsync();

    /// <summary>
    /// Performs a GET request and returns the response as a string.
    /// </summary>
    protected async Task<string> GetAsync(string endpoint)
    {
        var response = await HttpClient.GetAsync($"{BaseUrl}{endpoint}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Performs a POST request with JSON body.
    /// </summary>
    protected async Task PostAsync(string endpoint, string body = "")
    {
        var jsonBody = string.IsNullOrEmpty(body) ? "{}" : $"\"{body}\"";
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync($"{BaseUrl}{endpoint}", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Performs a PUT request with JSON body.
    /// </summary>
    protected async Task PutAsync(string endpoint, string body = "")
    {
        var jsonBody = string.IsNullOrEmpty(body) ? "{}" : $"\"{body}\"";
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await HttpClient.PutAsync($"{BaseUrl}{endpoint}", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public virtual void Dispose()
    {
        HttpClient?.Dispose();
    }
}
