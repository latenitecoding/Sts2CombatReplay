namespace CombatReplay.CombatReplayCode.Utils;

public class ReplayLogger(int profileId, bool isMultiplayer, string saveFile, bool loadSave)
{
    private string? _savePath;
    
    private readonly Lock _writerLock = new();
    private StreamWriter? _writer;

    public void OnRunStart()
    {
        _savePath = FileUtils.GetSavePath(profileId, isMultiplayer, saveFile);
        lock (_writerLock)
        {
            if (loadSave)
            {
                MainFile.Logger.Info($"Using existing save @ `{_savePath}`");
                _writer = new StreamWriter(_savePath, append: true);
                WriteIt("@@@ Loaded **save** @@@\\");
                return;
            }
            if (File.Exists(_savePath)) MainFile.Logger.Info($"Overwriting existing save @ `{_savePath}`");
            _writer = new StreamWriter(_savePath, append: false);
        }
    }

    public void OnRunEnd(long startTime)
    {
        lock (_writerLock)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer = null;
        }
        
        var finalPath = FileUtils.GetHistoryPath(profileId, startTime, saveFile);
        if (_savePath == null || finalPath == null || !File.Exists(_savePath)) return;
        
        File.Move(_savePath, finalPath, overwrite: true);
        MainFile.Logger.Info($"Combat history saved to: {finalPath}");
    }
    
    public void WriteIt(string msg)
    {
        if (_savePath == null || _writer == null) return;
        lock (_writerLock)
        {
            _writer.WriteLine(msg);
            _writer.Flush();
        }
    }
}