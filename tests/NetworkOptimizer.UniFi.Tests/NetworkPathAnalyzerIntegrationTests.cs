using FluentAssertions;
using NetworkOptimizer.UniFi.Tests.Fixtures;
using Xunit;

namespace NetworkOptimizer.UniFi.Tests;

/// <summary>
/// Tests for stale wireless client detection scenarios.
/// These tests verify the conditions that trigger the "wireless client with no AP MAC" detection.
///
/// Note: Full integration testing of NetworkPathAnalyzer.CalculatePathAsync would require
/// mocking IUniFiClientProvider with a mockable UniFiApiClient interface. Current tests
/// validate the data model conditions that trigger the fix.
/// </summary>
public class StaleWirelessClientDetectionTests
{
    /// <summary>
    /// Verifies the stale client fixture has the expected "missing AP MAC" condition.
    /// This is the condition that triggers the fix in NetworkPathAnalyzer.BuildHopList.
    /// </summary>
    [Fact]
    public void StaleWirelessClient_HasNoApMac()
    {
        // Arrange & Act
        var staleClient = NetworkTestData.CreateStaleWirelessClient();

        // Assert - Should have null AP MAC and no wireless stats
        staleClient.ConnectedToDeviceMac.Should().BeNull();
        staleClient.IsWired.Should().BeFalse();
        staleClient.TxRate.Should().Be(0);
        staleClient.RxRate.Should().Be(0);
        staleClient.Radio.Should().BeNull();
    }

    /// <summary>
    /// Verifies the condition that triggers incomplete path detection:
    /// wireless client with no ConnectedToDeviceMac (AP MAC).
    /// </summary>
    [Fact]
    public void StaleWirelessClient_TriggersIncompletePathCondition()
    {
        // Arrange
        var staleClient = NetworkTestData.CreateStaleWirelessClient();

        // Act - This is the exact condition checked in NetworkPathAnalyzer.BuildHopList
        bool isWirelessWithNoAp = !staleClient.IsWired && string.IsNullOrEmpty(staleClient.ConnectedToDeviceMac);

        // Assert
        isWirelessWithNoAp.Should().BeTrue("stale wireless client should trigger the incomplete path detection");
    }

    /// <summary>
    /// Verifies that a normal wireless client does NOT trigger the incomplete path condition.
    /// </summary>
    [Fact]
    public void NormalWirelessClient_DoesNotTriggerIncompletePathCondition()
    {
        // Arrange
        var normalClient = NetworkTestData.CreateWirelessClient();

        // Act - Same condition as above
        bool isWirelessWithNoAp = !normalClient.IsWired && string.IsNullOrEmpty(normalClient.ConnectedToDeviceMac);

        // Assert
        isWirelessWithNoAp.Should().BeFalse("normal wireless client should have AP MAC and not trigger detection");
        normalClient.ConnectedToDeviceMac.Should().Be(NetworkTestData.ApWiredMac);
    }

    /// <summary>
    /// Verifies that a wired client does NOT trigger the incomplete path condition.
    /// </summary>
    [Fact]
    public void WiredClient_DoesNotTriggerIncompletePathCondition()
    {
        // Arrange
        var wiredClient = NetworkTestData.CreateWiredClient();

        // Act
        bool isWirelessWithNoAp = !wiredClient.IsWired && string.IsNullOrEmpty(wiredClient.ConnectedToDeviceMac);

        // Assert - Wired client should not trigger (IsWired is true)
        isWirelessWithNoAp.Should().BeFalse();
        wiredClient.IsWired.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that MLO client does NOT trigger the incomplete path condition.
    /// </summary>
    [Fact]
    public void MloClient_DoesNotTriggerIncompletePathCondition()
    {
        // Arrange
        var mloClient = NetworkTestData.CreateMloClient();

        // Act
        bool isWirelessWithNoAp = !mloClient.IsWired && string.IsNullOrEmpty(mloClient.ConnectedToDeviceMac);

        // Assert
        isWirelessWithNoAp.Should().BeFalse("MLO client should have AP MAC");
        mloClient.IsMlo.Should().BeTrue();
        mloClient.ConnectedToDeviceMac.Should().NotBeNullOrEmpty();
    }
}
