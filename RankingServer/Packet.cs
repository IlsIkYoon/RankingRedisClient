


using System.Diagnostics;

class CPacket
{
    private const int bufferSize = 1024;
    private const int packetStartPtr = 5;
    private const int headerStartPtr = 3;
    public byte[] buffer = new byte[bufferSize];
    public int front = packetStartPtr;
    public int rear = packetStartPtr;

    public bool EnqueueData(byte[] data, int length)
    {
        if (rear + length >= bufferSize)
        {
            Debugger.Break();
            return false;
        }
        Buffer.BlockCopy(data, 0, buffer, rear, length);

        return true;
    }

    public bool DequeueData(byte[] data, int length)
    {
        if (rear - front < length)
        {
            Debugger.Break();
        }

        Buffer.BlockCopy(buffer, front, data, 0, length);

        return true;
    }

    public bool MoveFront(int length)
    {
        if (front + length > rear)
        {
            Debugger.Break();
            return false;
        }

        front += length;

        return true;
    }

    public int GetSize()
    {
        return bufferSize - rear;
    }
    public ArraySegment<byte> GetEnqueueBuffer()
    {
        return new ArraySegment<byte>(buffer, rear, GetSize());
    }
    public bool MoveRear(int length)
    {
        if (rear + length >= bufferSize)
        {
            // 오류 처리: 버퍼 오버플로우
            Debugger.Break();
            return false;
        }
        rear += length;
        return true;
    }

    public void MakeNetworkHeader()
    {
        byte[] networkLen = BitConverter.GetBytes((ushort)GetSize());
        front = headerStartPtr;
        Buffer.BlockCopy(networkLen, 0, buffer, front, 2);
    }

    public void InitPacket() //남은 데이터 앞으로 땡겨주는 함수
    {
        int currentSize = GetSize();
        byte[] tempBuffer = new byte[currentSize];
        Buffer.BlockCopy(buffer, front, tempBuffer, 0, currentSize);
        Buffer.BlockCopy(tempBuffer, 0, buffer, 0, currentSize);
        front = 0;
        rear = currentSize;
    }

    public void RestorePacket(byte[] input, int len) //여기서 default작업 해줘야 하는데..
    {
        int currentSize = GetSize();
        byte[] tempBuffer = new byte[currentSize];
        Buffer.BlockCopy(buffer, front, tempBuffer, 0, currentSize);
        Buffer.BlockCopy(input, 0, buffer, packetStartPtr, len);
        Buffer.BlockCopy(tempBuffer, 0, buffer, len + packetStartPtr, currentSize);
        front = packetStartPtr;
        rear = currentSize + len + packetStartPtr;
    }

    public ArraySegment<byte> GetBuffer()
    {
        int currentDataSize = GetSize();

        return new ArraySegment<byte>(this.buffer, this.front, currentDataSize);
    }

}