namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// One row of the session summary grid: light frames grouped by canonical filter and date.
    /// </summary>
    internal sealed record SummaryRow(
        string Filter,
        string Date,
        int? Frames,
        double? TotalExposure,
        double? AvgCcdTemp,
        string Camera);
}
