

public class MyThread
{
    static int index = 0;
    public static int GetMyIndex()
    {
        return Interlocked.Increment(ref index);
    }
}

public class Session
{
    public uint level;
    public UInt64 ID;

    private const int maxLevel = 10000;

    public bool LevelUp()
    {
        if (level == maxLevel)
        {
            return false;
        }
        else
        {
            level++;
            return true;
        }
    }

}

public class SessionManager
{
    private uint maxSessionCount;
    private Session[] sessionList;

    public SessionManager(uint maxSessionCount)
    {
        this.maxSessionCount = maxSessionCount;
        int myIndex = MyThread.GetMyIndex();
        int myStartID = myIndex * (int)maxSessionCount;

        sessionList = new Session[maxSessionCount];
        for (int i = 0; i < maxSessionCount; i++)
        {
            sessionList[i] = new Session();
            sessionList[i].ID = (ulong)(myStartID++);
        }
    }
    ~SessionManager()
    {

    }

    public bool RandomPlayerLevelUp(out UInt64 id, out uint currentlevel)
    {
        Random rand = new Random();
        int currentIndex = rand.Next((int)maxSessionCount);
        bool levelUpRetval = sessionList[currentIndex].LevelUp();
        if (levelUpRetval == false)
        {
            id = 0;
            currentlevel = 0;
            return false;
        }
        else
        {
            id = sessionList[currentIndex].ID;
            currentlevel = sessionList[currentIndex].level;
            return true;
        }
    }


}