namespace RCleaner
{
    public interface ICleaner
    {
        void CleanUserTemp();
        void CleanWindowsTemp();
        void EmptyRecycleBin();
        void ScanAndReport();
        void FlushDns();
        void ClearNetworkCache();
    }
}
