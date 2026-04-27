namespace CombatReplay.CombatReplayCode.Utils;

public class ReplayLogger(int profileId, bool isMultiplayer, string saveFile, bool loadSave, string multiRunId = "mp")
{
    private string? _savePath;
    
    private readonly Lock _writerLock = new();
    private StreamWriter? _writer;

    private readonly LinkedList<BufferedMsg> _bufferStack = [];

    [Flags]
    public enum MsgType
    {
        None = 0,
        BeforeDamage = 1 << 0,
        BlockBroken = 1 << 1,
        CardAdded = 1 << 2,
        CardCreated = 1 << 3,
        CardDrawn = 1 << 4,
        CardGiven = 1 << 5,
        Defeated = 1 << 6,
        OrbEvoked = 1 << 7,
        PetWasHit = 1 << 8,
        PowerCleared = 1 << 9,
        RpoHit = 1 << 10, // Relic-Power/Potion-Orb Hit
        TookDamage = 1 << 11
    }

    public static MsgType AllCardTypes()
    {
        return MsgType.CardAdded | MsgType.CardCreated | MsgType.CardDrawn | MsgType.CardGiven;
    }

    public static bool Matches(MsgType? flags, MsgType flag)
    {
        if (flags == null) return false;
        if (flags == 0) return flag == 0;
        return (flags & flag) > 0;
    }
    
    private record BufferedMsg(string Msg, MsgType MsgType);

    public (bool ok, bool found) BufferBefore(string msg, MsgType msgType, MsgType preceded, bool autoFlush = true)
    {
        lock (_writerLock)
        {
            if (_bufferStack.Count == 0)
            {
                _bufferStack.AddLast(new BufferedMsg(msg, msgType));
                return (true, false);
            }

            for (var cur = _bufferStack.Last; cur != null; cur = cur.Previous)
            {
                if (!Matches(cur.Value.MsgType, preceded)) continue;
                _bufferStack.AddBefore(cur, new BufferedMsg(msg, msgType));
                return (true, true);
            }
        }
        
        if (autoFlush) Flush();
        
        lock (_writerLock)
        {
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
        }

        return (true, false);
    }

    public (bool ok, bool found) BufferIt(string msg, MsgType msgType, MsgType overwrite = MsgType.None, bool autoFlush = true)
    {
        if (overwrite != MsgType.None)
        {
            lock (_writerLock)
            {
                for (var cur = _bufferStack.Last; cur != null; cur = cur.Previous)
                {
                    if (!Matches(cur.Value.MsgType, overwrite)) continue;
                    cur.Value = new BufferedMsg(msg, msgType);
                    return (true, true);
                }
            }
        }

        // don't keep unrelated events together in the same buffer if autoFlush is set
        if (autoFlush) Flush();

        lock (_writerLock)
        {
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
        }

        return (true, false);
    }

    public void Flush()
    {
        if (_savePath == null || _writer == null) return;
        lock (_writerLock)
        {
            if (_bufferStack.Count == 0) return;
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
        _savePath = FileUtils.GetSavePath(profileId, isMultiplayer, saveFile, multiRunId);
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

    public (bool ok, bool found) WriteBefore(string msg, MsgType preceded)
    {
        if (_savePath == null || _writer == null) return (false, false);
        lock (_writerLock)
        {
            while (_bufferStack.Count > 0 && !Matches(_bufferStack.First().MsgType, preceded))
            {
                _writer.WriteLine(_bufferStack.First().Msg);
                _bufferStack.RemoveFirst();
            }

            var found = _bufferStack.Count > 0;

            _writer.WriteLine(msg);
            _writer.Flush();

            return (true, found);
        }
    }
    
    public (bool ok, bool found) WriteIt(string msg, MsgType overwrite = MsgType.None)
    {
        if (_savePath == null || _writer == null) return (false, false);
        if (overwrite != MsgType.None)
        {
            var (ok, found) = BufferIt(msg, MsgType.None, overwrite, autoFlush: false);
            Flush();
            return (ok, found);
        }
        
        Flush();
        
        lock (_writerLock)
        {
            _writer.WriteLine(msg);
            _writer.Flush();
        }

        return (true, false);
    }
}