namespace CombatReplay.CombatReplayCode.Utils;

public class ReplayLogger(int profileId, bool isMultiplayer, string saveFile, bool loadSave, string multiRunId = "mp")
{
    private string? _savePath;
    
    private readonly Lock _writerLock = new();
    private StreamWriter? _writer;

    private readonly LinkedList<BufferedMsg> _bufferStack = [];
    private MsgType _expectedType = MsgType.None;

    [Flags]
    public enum MsgType
    {
        None = 0,
        ActStarted = 1 << 0,
        BeforeDamage = 1 << 1,
        BlockBroken = 1 << 2,
        CardAdded = 1 << 3,
        CardCreated = 1 << 4,
        CardDrawn = 1 << 5,
        CardEntering = 1 << 6,
        CardGiven = 1 << 7,
        GainMaxHp = 1 << 8,
        HealCreature = 1 << 9,
        OrbChanneled = 1 << 10,
        OrbEvoked = 1 << 11,
        PetWasHit = 1 << 12,
        PlayerOrEnemyWasHit = 1 << 13,
        PowerApplied = 1 << 14,
        PowerCleared = 1 << 15,
        ReviveCreature = 1 << 16,
        RoomEntered = 1 << 17,
        RoomExited = 1 << 18,
        RpoHit = 1 << 19, // Relic-Power/Potion-Orb Hit
        RunEnded = 1 << 20,
        Summon = 1 << 21,
        TookDamage = 1 << 22,
        Any = ~None,
    }
    
    public static bool Matches(MsgType? flags, MsgType flag)
    {
        if (flags == null) return false;
        if (flags == 0) return flag == 0;
        return (flags & flag) > 0;
    }
    
    private record BufferedMsg(string Msg, MsgType MsgType);
    
    private void CheckExpectedTypeUnsafe(MsgType msgType, MsgType expectingType)
    {
        // do not call without first acquiring a lock
        var flushIt = _bufferStack.Count > 0 && !Matches(_expectedType, msgType);
        _expectedType = expectingType;

        if (flushIt) FlushUnsafe();
    }

    private void FlushUnsafe()
    {
        // do not call without first acquiring a lock
        if (_savePath == null || _writer == null || _bufferStack.Count == 0) return;
        
        while (_bufferStack.Count > 0)
        {
            _writer.WriteLine(_bufferStack.First().Msg);
            _bufferStack.RemoveFirst();
        }
        
        _writer.Flush();
    }

    public (bool ok, bool found) BufferBefore(string msg, MsgType msgType, MsgType preceding, MsgType expecting = MsgType.Any)
    {
        if (expecting == MsgType.None) throw new Exception("Should call WriteBefore instead of BufferBefore expecting None");
        if (_savePath == null || _writer == null) return (false, false);
        
        lock (_writerLock)
        {
            CheckExpectedTypeUnsafe(msgType, expecting);

            for (var cur = _bufferStack.First; cur != null; cur = cur.Next)
            {
                if (!Matches(cur.Value.MsgType, preceding)) continue;
                _bufferStack.AddBefore(cur, new BufferedMsg(msg, msgType));
                return (true, true);
            }
            
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
            return (true, false);
        }
    }

    public (bool ok, bool found) BufferIt(string msg, MsgType msgType, MsgType overwriting = MsgType.None, MsgType expecting = MsgType.Any)
    {
        if (expecting == MsgType.None) throw new Exception("Should call WriteIt instead of BufferIt expecting None");
        if (_savePath == null || _writer == null) return (false, false);

        lock (_writerLock)
        {
            CheckExpectedTypeUnsafe(msgType, expecting);

            if (overwriting != MsgType.None)
            {
                for (var cur = _bufferStack.Last; cur != null; cur = cur.Previous)
                {
                    if (!Matches(cur.Value.MsgType, overwriting)) continue;
                    cur.Value = new BufferedMsg(msg, msgType);
                    return (true, true);
                }
            }
            
            _bufferStack.AddLast(new BufferedMsg(msg, msgType));
            return (true, false);
        }
    }

    public bool CheckIt(MsgType msgType) => Matches(PeekBufferType(), msgType);
    
    public void Flush()
    {
        if (_savePath == null || _writer == null) return;

        lock (_writerLock)
        {
            FlushUnsafe();
        }
    }

    public void OnCombatEnd() => Flush();

    public void OnRunEnd(long startTime)
    {
        if (_savePath == null) return;
        
        lock (_writerLock)
        {
            FlushUnsafe();
            _writer?.Flush();
            _writer?.Close();
            _writer = null;
        }
        
        var finalPath = FileUtils.GetHistoryPath(profileId, startTime, saveFile.Replace("_current", ""));
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

    public MsgType PeekBufferType()
    {
        lock (_writerLock)
        {
            return _bufferStack.Count > 0 ? _bufferStack.Last().MsgType : MsgType.None;
        }
    }

    public (bool ok, bool found) WriteBefore(string msg, MsgType preceding)
    {
        if (_savePath == null || _writer == null) return (false, false);
        
        lock (_writerLock)
        {
            _expectedType = MsgType.None;
            
            while (_bufferStack.Count > 0 && !Matches(_bufferStack.First().MsgType, preceding))
            {
                _writer.WriteLine(_bufferStack.First().Msg);
                _bufferStack.RemoveFirst();
            }

            var found = _bufferStack.Count > 0;

            _writer.WriteLine(msg);
            FlushUnsafe();

            return (true, found);
        }
    }
    
    public (bool ok, bool found) WriteIt(string msg, MsgType overwriting = MsgType.None)
    {
        if (_savePath == null || _writer == null) return (false, false);

        lock (_writerLock)
        {
            _expectedType = MsgType.None;

            var foundIt = false;
            
            if (overwriting != MsgType.None)
            {
                for (var cur = _bufferStack.Last; cur != null; cur = cur.Previous)
                {
                    if (!Matches(cur.Value.MsgType, overwriting)) continue;
                    cur.Value = new BufferedMsg(msg, MsgType.None);
                    foundIt = true;
                    break;
                }
            }
            
            if (!foundIt) _writer.WriteLine(msg);
            FlushUnsafe();

            return (true, foundIt);
        }
    }
}