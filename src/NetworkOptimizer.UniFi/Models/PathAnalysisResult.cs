namespace NetworkOptimizer.UniFi.Models;

/// <summary>
/// Result of analyzing a speed test against the network path.
/// Combines path information with performance grading.
/// </summary>
public class PathAnalysisResult
{
    /// <summary>The network path from iperf3 server to target device</summary>
    public NetworkPath Path { get; set; } = new();

    /// <summary>Measured throughput from device to server (Mbps)</summary>
    public double MeasuredFromDeviceMbps { get; set; }

    /// <summary>Measured throughput to device from server (Mbps)</summary>
    public double MeasuredToDeviceMbps { get; set; }

    /// <summary>TCP retransmits from device to server (upload direction)</summary>
    public int FromDeviceRetransmits { get; set; }

    /// <summary>TCP retransmits to device from server (download direction)</summary>
    public int ToDeviceRetransmits { get; set; }

    /// <summary>Efficiency of from-device transfer vs theoretical max (%)</summary>
    public double FromDeviceEfficiencyPercent { get; set; }

    /// <summary>Efficiency of to-device transfer vs theoretical max (%)</summary>
    public double ToDeviceEfficiencyPercent { get; set; }

    /// <summary>Performance grade for from-device transfer</summary>
    public PerformanceGrade FromDeviceGrade { get; set; }

    /// <summary>Performance grade for to-device transfer</summary>
    public PerformanceGrade ToDeviceGrade { get; set; }

    /// <summary>Observations about the test results</summary>
    public List<string> Insights { get; set; } = new();

    /// <summary>Suggestions for improving performance</summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Calculate efficiency and grade based on measured vs theoretical speeds
    /// </summary>
    public void CalculateEfficiency()
    {
        if (Path.RealisticMaxMbps > 0)
        {
            FromDeviceEfficiencyPercent = (MeasuredFromDeviceMbps / Path.RealisticMaxMbps) * 100;
            ToDeviceEfficiencyPercent = (MeasuredToDeviceMbps / Path.RealisticMaxMbps) * 100;

            FromDeviceGrade = GetGrade(FromDeviceEfficiencyPercent);
            ToDeviceGrade = GetGrade(ToDeviceEfficiencyPercent);
        }
    }

    private static PerformanceGrade GetGrade(double efficiencyPercent) => efficiencyPercent switch
    {
        >= 90 => PerformanceGrade.Excellent,
        >= 75 => PerformanceGrade.Good,
        >= 50 => PerformanceGrade.Fair,
        >= 25 => PerformanceGrade.Poor,
        _ => PerformanceGrade.Critical
    };

    /// <summary>
    /// Generate insights based on the analysis.
    /// Note: Path info (routing, bottleneck) shown separately in UI - don't duplicate here.
    /// </summary>
    public void GenerateInsights()
    {
        Insights.Clear();
        Recommendations.Clear();

        // Gateway tests have inherent CPU overhead - note this and skip performance warnings
        if (Path.TargetIsGateway)
        {
            Insights.Add("Gateway speed test - results limited by gateway CPU, not network");
            // Skip other performance-based insights for gateway tests
            return;
        }

        // AP tests are CPU-limited; anything above ~4.4 Gbps is considered good
        const double ApGoodSpeedThreshold = 4400;
        bool apPerformingWell = Path.TargetIsAccessPoint &&
            (MeasuredFromDeviceMbps >= ApGoodSpeedThreshold || MeasuredToDeviceMbps >= ApGoodSpeedThreshold);

        if (Path.TargetIsAccessPoint && apPerformingWell)
        {
            Insights.Add("AP speed test - results limited by AP CPU, not network");
            return;
        }

        // Wireless connection warning (client->AP or AP->AP, not AP->Switch)
        if (Path.HasWirelessConnection)
        {
            Insights.Add("Path includes wireless segment - speeds may vary with signal quality");
        }

        // Performance-based insights (note: enum comparison - higher value = worse grade)
        var avgEfficiency = (FromDeviceEfficiencyPercent + ToDeviceEfficiencyPercent) / 2;

        if (FromDeviceGrade >= PerformanceGrade.Poor || ToDeviceGrade >= PerformanceGrade.Poor)
        {
            Insights.Add("Performance below expected - possible congestion or network issue");

            if (Math.Abs(FromDeviceEfficiencyPercent - ToDeviceEfficiencyPercent) > 20)
            {
                Recommendations.Add("Large asymmetry detected - check for half-duplex links or congestion");
            }
        }
        else if (FromDeviceGrade == PerformanceGrade.Fair || ToDeviceGrade == PerformanceGrade.Fair)
        {
            Insights.Add("Performance is moderate - some overhead or minor congestion");
        }

        // Recommendations based on bottleneck
        if (Path.TheoreticalMaxMbps <= 100)
        {
            Recommendations.Add("100 Mbps link detected - consider upgrading to gigabit");
        }
        else if (Path.TheoreticalMaxMbps == 1000 && avgEfficiency >= 90)
        {
            Recommendations.Add("Maxing out 1 GbE - consider 2.5G or 10G upgrade for higher speeds");
        }

        // Retransmit analysis
        AnalyzeRetransmits();
    }

    /// <summary>
    /// Analyze TCP retransmits and generate insights about packet loss
    /// </summary>
    private void AnalyzeRetransmits()
    {
        var totalRetransmits = FromDeviceRetransmits + ToDeviceRetransmits;

        // Skip if no retransmits
        if (totalRetransmits == 0)
            return;

        // Threshold for "high" retransmits - based on test duration and speed
        // For a 10-second test at 1 Gbps, thousands of retransmits is concerning
        const int HighRetransmitThreshold = 100;
        const int VeryHighRetransmitThreshold = 1000;

        // Check for asymmetric retransmits (significant difference between directions)
        var hasFromRetransmits = FromDeviceRetransmits > 0;
        var hasToRetransmits = ToDeviceRetransmits > 0;

        if (hasFromRetransmits != hasToRetransmits && totalRetransmits >= HighRetransmitThreshold)
        {
            // One direction has retransmits, the other doesn't
            if (hasToRetransmits && !hasFromRetransmits)
            {
                Insights.Add($"High packet loss on download path ({ToDeviceRetransmits:N0} retransmits)");
                if (Path.HasWirelessConnection)
                {
                    Recommendations.Add("Download retransmits on wireless - check for interference or weak signal");
                }
            }
            else if (hasFromRetransmits && !hasToRetransmits)
            {
                Insights.Add($"High packet loss on upload path ({FromDeviceRetransmits:N0} retransmits)");
                if (Path.HasWirelessConnection)
                {
                    Recommendations.Add("Upload retransmits on wireless - may indicate mesh uplink contention");
                }
            }
        }
        else if (totalRetransmits >= VeryHighRetransmitThreshold)
        {
            // Both directions have significant retransmits
            Insights.Add($"High packet loss detected ({totalRetransmits:N0} total retransmits)");
            Recommendations.Add("Check for network congestion, interference, or faulty cables");
        }
        else if (totalRetransmits >= HighRetransmitThreshold)
        {
            // Moderate retransmits
            Insights.Add($"Moderate packet loss ({totalRetransmits:N0} retransmits)");
        }
    }
}

/// <summary>
/// Performance grade based on efficiency percentage
/// </summary>
public enum PerformanceGrade
{
    /// <summary>90%+ of theoretical maximum</summary>
    Excellent,

    /// <summary>75-89% of theoretical maximum</summary>
    Good,

    /// <summary>50-74% of theoretical maximum</summary>
    Fair,

    /// <summary>25-49% of theoretical maximum</summary>
    Poor,

    /// <summary>Under 25% of theoretical maximum</summary>
    Critical
}
