namespace CombatReplay.CombatReplayCode.Utils;

public class ReplayLogger(int profileId, bool isMultiplayer, string saveFile, bool loadSave)
{
    private string? _savePath;
    
    private readonly Lock _writerLock = new();
    private StreamWriter? _writer;

    private readonly LinkedList<BufferedMsg> _bufferStack = [];

    public enum MsgType
    {
        BlockBroken,
        CardAdded,
        CardCreated,
        CardEntered,
        CardGiven,
        Draw,
        None,
        OrbEvoked,
        PetWasHit,
        RpoHit // Relic-Power-Orb Hit
    }
    
    private record BufferedMsg(string Msg, MsgType MsgType);

    public void BufferBefore(string msg, MsgType msgType, MsgType preceded)
    {
        if (_bufferStack.Count == 0)
        {
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
            return;
        }
        
        var tmp = new Stack<BufferedMsg>();
        while (_bufferStack.Count > 0)
        {
            tmp.Push(_bufferStack.Last());
            _bufferStack.RemoveLast();
            if (tmp.Peek().MsgType != preceded) continue;
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
            while (tmp.Count > 0) _bufferStack.AddLast(tmp.Pop());
            return;
        }

        while (tmp.Count > 0) _bufferStack.AddLast(tmp.Pop());
        Flush();
        _bufferStack.AddLast(new BufferedMsg(msg, msgType));
    }

    public void BufferIt(string msg, MsgType msgType, MsgType overwrite = MsgType.None)
    {
        if (overwrite != MsgType.None && PeekBufferType() == overwrite)
        {
            _bufferStack.RemoveLast();
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
            return;
        }

        // don't keep unrelated events together in the same buffer
        Flush();
        _bufferStack.AddLast(new BufferedMsg(msg, msgType));
    }

    public void Flush()
    {
        if (_savePath == null || _writer == null || _bufferStack.Count == 0) return;
        lock (_writerLock)
        {
            while (_bufferStack.Count > 0)
            {
                _writer.WriteLine(_bufferStack.First().Msg);
                _bufferStack.RemoveFirst();
            }
            _writer.Flush();
        }
    }

    public void OnCombatEnd() => Flush();

    public void OnRunEnd(long startTime)
    {
        if (_savePath == null) return;
        
        Flush();
        lock (_writerLock)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer = null;
        }
        
        var finalPath = FileUtils.GetHistoryPath(profileId, startTime, saveFile);
        if (finalPath == null || !File.Exists(_savePath)) return;
        
        File.Move(_savePath, finalPath, overwrite: true);
        MainFile.Logger.Info($"Combat history saved to: {finalPath}");
    }
    
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

    public MsgType PeekBufferType() => _bufferStack.Count > 0 ? _bufferStack.Last().MsgType : MsgType.None;

    public void WriteBefore(string msg, MsgType msgType)
    {
        if (_savePath == null || _writer == null) return;
        lock (_writerLock)
        {
            while (_bufferStack.Count > 0 && _bufferStack.First().MsgType != msgType)
            {
                _writer.WriteLine(_bufferStack.First().Msg);
                _bufferStack.RemoveFirst();
            }
            _writer.WriteLine(msg);
            _writer.Flush();
        }
    }
    
    public void WriteIt(string msg)
    {
        if (_savePath == null || _writer == null) return;
        Flush();
        lock (_writerLock)
        {
            _writer.WriteLine(msg);
            _writer.Flush();
        }
    }
}