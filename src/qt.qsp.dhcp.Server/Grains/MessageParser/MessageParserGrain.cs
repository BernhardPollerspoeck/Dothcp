using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;

namespace qt.qsp.dhcp.Server.Grains.MessageParser;

public class MessageParserGrain : Grain, IMessageParserGrain
{
    #region IMessageParserGrain
    public Task<DhcpMessage> Parse(byte[] buffer)
    {
        return Task.FromResult(new DhcpMessage
        {
            Direction = (EMessageDirection)buffer[0],
            HardwareType = (EHardwareType)buffer[1],
            ClientIdLength = buffer[2],
            Hops = buffer[3],
            TransactionId = BitConverter.ToUInt32(buffer, 4),
            ResponseCastType = (EResponseCastType)BitConverter.ToUInt16(buffer, 8),
            ClientIpAdress = BitConverter.ToUInt32(buffer, 10),
            AssigneeAdress = BitConverter.ToUInt32(buffer, 14),
            ServerIpAdress = BitConverter.ToUInt32(buffer, 18),
            ClientHardwareAdress = buffer[22..38],
            Options = ReadOptions(buffer[240..])
        });
    }
    #endregion

    #region readers
    private static DhcpOption[] ReadOptions(byte[] optionsBuffer)
    {
        var bufferQueue = new Queue<byte>(optionsBuffer);
        var options = new List<DhcpOption>();

        while (bufferQueue.Count > 0)
        {
            var option = (EOption)bufferQueue.Dequeue();
            if (option is EOption.End)
            {
                break;
            }
            var length = bufferQueue.Dequeue();
            var data = Enumerable
                .Range(1, length)
                .Select(r => bufferQueue.Dequeue())
                .ToArray();
            options.Add(new DhcpOption
            {
                Option = option,
                Data = data,
            });
        }

        return [.. options];
    }
    #endregion
}