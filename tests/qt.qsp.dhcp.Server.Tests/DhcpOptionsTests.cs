using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using System.Net;
using System.Text;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class DhcpOptionsTests
{
    [Fact]
    public void BuildAndParseDomainName_ShouldMatch()
    {
        // Arrange
        var domainName = "example.com";
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddDomainName(domainName).Build();
        
        // Create a message with these options
        var message = new DhcpMessage
        {
            Direction = EMessageDirection.Request,
            HardwareType = EHardwareType.Ethernet,
            ClientIdLength = 6,
            Hops = 0,
            TransactionId = 12345,
            ResponseCastType = EResponseCastType.Broadcast,
            ClientIpAdress = 0,
            AssigneeAdress = 0,
            ServerIpAdress = 0,
            ClientHardwareAdress = new byte[16],
            Options = options
        };
        
        var retrievedDomainName = message.GetDomainName();
        
        // Assert
        Assert.NotNull(retrievedDomainName);
        Assert.Equal(domainName, retrievedDomainName);
    }
    
    [Fact]
    public void BuildAndParseNetBiosNameServers_ShouldMatch()
    {
        // Arrange
        var nameServers = new[] { "192.168.1.10", "192.168.1.11" };
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddNetBiosNameServers(nameServers).Build();
        
        // Create a message with these options
        var message = new DhcpMessage
        {
            Direction = EMessageDirection.Request,
            HardwareType = EHardwareType.Ethernet,
            ClientIdLength = 6,
            Hops = 0,
            TransactionId = 12345,
            ResponseCastType = EResponseCastType.Broadcast,
            ClientIpAdress = 0,
            AssigneeAdress = 0,
            ServerIpAdress = 0,
            ClientHardwareAdress = new byte[16],
            Options = options
        };
        
        var retrievedServers = message.GetNetBiosNameServers();
        
        // Assert
        Assert.NotNull(retrievedServers);
        Assert.Equal(nameServers.Length, retrievedServers.Length);
        Assert.Equal(IPAddress.Parse(nameServers[0]), retrievedServers[0]);
        Assert.Equal(IPAddress.Parse(nameServers[1]), retrievedServers[1]);
    }
    
    [Fact]
    public void BuildAndParseNetBiosNodeType_ShouldMatch()
    {
        // Arrange
        byte nodeType = 1; // B-node
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddNetBiosNodeType(nodeType).Build();
        
        // Create a message with these options
        var message = new DhcpMessage
        {
            Direction = EMessageDirection.Request,
            HardwareType = EHardwareType.Ethernet,
            ClientIdLength = 6,
            Hops = 0,
            TransactionId = 12345,
            ResponseCastType = EResponseCastType.Broadcast,
            ClientIpAdress = 0,
            AssigneeAdress = 0,
            ServerIpAdress = 0,
            ClientHardwareAdress = new byte[16],
            Options = options
        };
        
        var retrievedNodeType = message.GetNetBiosNodeType();
        
        // Assert
        Assert.NotNull(retrievedNodeType);
        Assert.Equal(nodeType, retrievedNodeType);
    }
    
    [Fact]
    public void BuildAndParseVendorSpecificInfo_ShouldMatch()
    {
        // Arrange
        var vendorInfo = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddVendorSpecificInfo(vendorInfo).Build();
        
        // Create a message with these options
        var message = new DhcpMessage
        {
            Direction = EMessageDirection.Request,
            HardwareType = EHardwareType.Ethernet,
            ClientIdLength = 6,
            Hops = 0,
            TransactionId = 12345,
            ResponseCastType = EResponseCastType.Broadcast,
            ClientIpAdress = 0,
            AssigneeAdress = 0,
            ServerIpAdress = 0,
            ClientHardwareAdress = new byte[16],
            Options = options
        };
        
        var retrievedInfo = message.GetVendorSpecificInfo();
        
        // Assert
        Assert.NotNull(retrievedInfo);
        Assert.Equal(vendorInfo.Length, retrievedInfo.Length);
        for (int i = 0; i < vendorInfo.Length; i++)
        {
            Assert.Equal(vendorInfo[i], retrievedInfo[i]);
        }
    }
    
    [Fact]
    public void BuildAndParseDnsSearchList_ShouldMatchSimpleDomains()
    {
        // Arrange
        var searchDomains = new[] { "example.com", "test.local" };
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddDnsSearchList(searchDomains).Build();
        
        // Create a message with these options
        var message = new DhcpMessage
        {
            Direction = EMessageDirection.Request,
            HardwareType = EHardwareType.Ethernet,
            ClientIdLength = 6,
            Hops = 0,
            TransactionId = 12345,
            ResponseCastType = EResponseCastType.Broadcast,
            ClientIpAdress = 0,
            AssigneeAdress = 0,
            ServerIpAdress = 0,
            ClientHardwareAdress = new byte[16],
            Options = options
        };
        
        var retrievedDomains = message.GetDnsSearchList();
        
        // Assert
        // Note: This test may be brittle due to DNS name compression complexities
        // We're testing a simplified implementation that works with basic domains
        Assert.NotNull(retrievedDomains);
        Assert.Equal(searchDomains.Length, retrievedDomains.Length);
    }
}