using FluentAssertions;
using Xunit;

namespace NetworkOptimizer.UniFi.Tests;

/// <summary>
/// Tests for CGNAT/Tailscale IP detection logic.
/// CGNAT range is 100.64.0.0/10 (100.64.0.0 - 100.127.255.255).
/// These IPs are used by Tailscale and other CGNAT providers and will never
/// appear in UniFi topology, so path analysis retries should be skipped.
/// </summary>
public class CgnatIpDetectionTests
{
    /// <summary>
    /// Checks if an IP is in the CGNAT range (100.64.0.0/10).
    /// This mirrors the logic in ClientSpeedTestService.IsNonRoutableIp.
    /// </summary>
    private static bool IsCgnatIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
            return false; // Different from IsNonRoutableIp which returns true for null

        if (ip.StartsWith("100."))
        {
            if (int.TryParse(ip.Split('.')[1], out int secondOctet))
            {
                if (secondOctet >= 64 && secondOctet <= 127)
                    return true;
            }
        }

        return false;
    }

    #region CGNAT Range Tests (100.64.0.0 - 100.127.255.255)

    [Theory]
    [InlineData("100.64.0.1")]      // Start of range
    [InlineData("100.64.255.255")]  // End of .64 subnet
    [InlineData("100.97.85.114")]   // Typical Tailscale IP
    [InlineData("100.100.100.100")] // Middle of range
    [InlineData("100.108.34.43")]   // Another Tailscale IP
    [InlineData("100.127.255.254")] // Near end of range
    [InlineData("100.127.255.255")] // End of range
    public void IsCgnatIp_CgnatRange_ReturnsTrue(string ip)
    {
        IsCgnatIp(ip).Should().BeTrue($"{ip} is in CGNAT range");
    }

    #endregion

    #region Non-CGNAT 100.x.x.x Tests

    [Theory]
    [InlineData("100.0.0.1")]       // Before CGNAT range
    [InlineData("100.63.255.255")]  // Just before CGNAT range
    [InlineData("100.128.0.0")]     // Just after CGNAT range
    [InlineData("100.200.50.25")]   // Well after CGNAT range
    [InlineData("100.255.255.255")] // End of 100.x.x.x
    public void IsCgnatIp_NonCgnat100Range_ReturnsFalse(string ip)
    {
        IsCgnatIp(ip).Should().BeFalse($"{ip} is NOT in CGNAT range");
    }

    #endregion

    #region Regular Private IPs

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("192.168.1.100")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    public void IsCgnatIp_PrivateIps_ReturnsFalse(string ip)
    {
        IsCgnatIp(ip).Should().BeFalse($"{ip} is a regular private IP");
    }

    #endregion

    #region Public IPs

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("142.250.80.46")]  // Google
    public void IsCgnatIp_PublicIps_ReturnsFalse(string ip)
    {
        IsCgnatIp(ip).Should().BeFalse($"{ip} is a public IP");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsCgnatIp_NullOrEmpty_ReturnsFalse(string? ip)
    {
        IsCgnatIp(ip).Should().BeFalse();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not.an.ip")]
    [InlineData("100")]
    [InlineData("100.")]
    [InlineData("100.abc.1.1")]
    public void IsCgnatIp_InvalidFormat_ReturnsFalse(string ip)
    {
        IsCgnatIp(ip).Should().BeFalse($"invalid IP format should return false");
    }

    #endregion
}
