using MegaCrit.Sts2.Core.GameActions;

namespace CombatReplay.CombatReplayCode.Utils;

public class ReplayLogger(int profileId, bool isMultiplayer, string saveFile, bool loadSave)
{
    private string? _savePath;
    
    private readonly Lock _writerLock = new();
    private StreamWriter? _writer;

    private LinkedList<BufferedMsg> bufferStack = new();

    public enum MsgType
    {
        BlockBroken,
        CardAdded,
        CardCreated,
        CardEntered,
        CardGiven,
        Draw,
        OrbEvoked,
        PetWasHit,
        RpoHit, // Relic-Power-Orb Hit
        None
    }
    
    private record BufferedMsg(String msg, MsgType msgType);

    public void BufferBefore(string msg, MsgType msgType, MsgType preceded)
    {
        if (bufferStack.Count == 0)
        {
            bufferStack.AddLast(new BufferedMsg(msg, msgType));
            return;
        }
        
        var tmp = new Stack<BufferedMsg>();
        while (bufferStack.Count > 0)
        {
            tmp.Push(bufferStack.Last());
            bufferStack.RemoveLast();
            if (tmp.Peek().msgType != preceded) continue;
            bufferStack.AddLast(new BufferedMsg(msg, msgType));
            break;
        }

        while (tmp.Count > 0) bufferStack.AddLast(tmp.Pop());
    }

    public void BufferIt(string msg, MsgType msgType, MsgType overwrite = MsgType.None)
    {
        if (overwrite != MsgType.None && GetBufferType() == overwrite)
        {
            bufferStack.RemoveLast();
            bufferStack.AddLast(new BufferedMsg(msg, msgType));
            return;
        }

        Flush();
        bufferStack.AddLast(new BufferedMsg(msg, msgType));
    }

    public void Flush()
    {
        if (_savePath == null || _writer == null || bufferStack.Count == 0) return;
        lock (_writerLock)
        {
            while (bufferStack.Count > 0)
            {
                _writer.WriteLine(bufferStack.First().msg);
                bufferStack.RemoveFirst();
            }
            _writer.Flush();
        }
    }

    public MsgType GetBufferType() => bufferStack.Count > 0 ? bufferStack.Last().msgType : MsgType.None;

    public void OnCombatEnd() => Flush();

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
        Flush();
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

    public void WriteBefore(string msg, MsgType msgType)
    {
        if (_savePath == null || _writer == null) return;
        lock (_writerLock)
        {
            while (bufferStack.Count > 0 && bufferStack.First().msgType != msgType)
            {
                _writer.WriteLine(bufferStack.First().msg);
                bufferStack.RemoveFirst();
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