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
    public void BuildAndParseNetBiosScope_ShouldMatch()
    {
        // Arrange
        var scope = "WORKGROUP";
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddNetBiosScope(scope).Build();
        
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
        
        var retrievedScope = message.GetNetBiosScope();
        
        // Assert
        Assert.NotNull(retrievedScope);
        Assert.Equal(scope, retrievedScope);
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
    public void BuildAndParseNtpServers_ShouldMatch()
    {
        // Arrange
        var ntpServers = new[] { "132.163.96.1", "132.163.97.1" };
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddNtpServerOptions(ntpServers).Build();
        
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
        
        var retrievedServers = message.GetNtpServers();
        
        // Assert
        Assert.NotNull(retrievedServers);
        Assert.Equal(ntpServers.Length, retrievedServers.Length);
        Assert.Equal(IPAddress.Parse(ntpServers[0]), retrievedServers[0]);
        Assert.Equal(IPAddress.Parse(ntpServers[1]), retrievedServers[1]);
    }
    
    [Fact]
    public void BuildAndParseRelayAgentInfo_ShouldMatch()
    {
        // Arrange
        var relayInfo = new byte[] { 0x01, 0x06, 0x00, 0x04, 0xAC, 0x11, 0x00, 0x01 };
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddRelayAgentInfo(relayInfo).Build();
        
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
        
        var retrievedInfo = message.GetRelayAgentInfo();
        
        // Assert
        Assert.NotNull(retrievedInfo);
        Assert.Equal(relayInfo.Length, retrievedInfo.Length);
        for (int i = 0; i < relayInfo.Length; i++)
        {
            Assert.Equal(relayInfo[i], retrievedInfo[i]);
        }
    }
    
    [Fact]
    public void BuildAndParseClasslessStaticRoutes_ShouldMatch()
    {
        // Arrange
        var routes = new Dictionary<(byte prefixLength, byte[] networkPrefix), IPAddress>
        {
            { (24, new byte[] { 10, 0, 0 }), IPAddress.Parse("192.168.1.1") },
            { (16, new byte[] { 192, 168 }), IPAddress.Parse("10.0.0.1") }
        };
        
        // Act
        var builder = new DhcpOptionsBuilder();
        var options = builder.AddClasslessStaticRoutes(routes).Build();
        
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
        
        var retrievedRoutes = message.GetClasslessStaticRoutes();
        
        // Assert
        Assert.NotNull(retrievedRoutes);
        Assert.Equal(routes.Count, retrievedRoutes.Count);
        
        // Verify routes match
        foreach (var route in routes)
        {
            bool foundMatch = false;
            foreach (var retrievedRoute in retrievedRoutes)
            {
                if (route.Key.prefixLength == retrievedRoute.Key.prefixLength &&
                    route.Value.Equals(retrievedRoute.Value))
                {
                    // Compare network prefix bytes
                    bool prefixMatches = true;
                    for (int i = 0; i < route.Key.networkPrefix.Length; i++)
                    {
                        if (route.Key.networkPrefix[i] != retrievedRoute.Key.networkPrefix[i])
                        {
                            prefixMatches = false;
                            break;
                        }
                    }
                    
                    if (prefixMatches)
                    {
                        foundMatch = true;
                        break;
                    }
                }
            }
            
            Assert.True(foundMatch, "Could not find matching static route in retrieved routes");
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
        Assert.NotNull(retrievedDomains);
        Assert.Equal(searchDomains.Length, retrievedDomains.Length);
        Assert.Contains("example.com", retrievedDomains);
        Assert.Contains("test.local", retrievedDomains);
    }
    
    [Fact]
    public void BuildAndParseDnsSearchList_WithCompressionPointers_ShouldMatch()
    {
        // Arrange
        var searchDomains = new[] { "example.com", "test.example.com", "sub.test.example.com" };
        
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
        Assert.NotNull(retrievedDomains);
        Assert.Equal(searchDomains.Length, retrievedDomains.Length);
        
        // Sort both arrays for comparison since order may not be preserved due to compression
        Array.Sort(searchDomains);
        var sortedRetrieved = retrievedDomains.OrderBy(d => d).ToArray();
        
        for (int i = 0; i < searchDomains.Length; i++)
        {
            Assert.Equal(searchDomains[i], sortedRetrieved[i]);
        }
    }
    
    [Fact]
    public void AddNtpServerOptions_WithNullArray_ShouldNotThrow()
    {
        // Arrange
        var builder = new DhcpOptionsBuilder();
        
        // Act & Assert - Should not throw exception
        var result = builder.AddNtpServerOptions(null);
        
        Assert.NotNull(result);
        var options = result.Build();
        
        // Should only contain the End option
        Assert.Single(options);
        Assert.Equal(EOption.End, options[0].Option);
    }
    
    [Fact]
    public void AddDnsServerOptions_WithNullArray_ShouldNotThrow()
    {
        // Arrange
        var builder = new DhcpOptionsBuilder();
        
        // Act & Assert - Should not throw exception
        var result = builder.AddDnsServerOptions(null);
        
        Assert.NotNull(result);
        var options = result.Build();
        
        // Should only contain the End option
        Assert.Single(options);
        Assert.Equal(EOption.End, options[0].Option);
    }
    
    [Fact]
    public void AddNetBiosNameServers_WithNullArray_ShouldNotThrow()
    {
        // Arrange
        var builder = new DhcpOptionsBuilder();
        
        // Act & Assert - Should not throw exception
        var result = builder.AddNetBiosNameServers(null);
        
        Assert.NotNull(result);
        var options = result.Build();
        
        // Should only contain the End option
        Assert.Single(options);
        Assert.Equal(EOption.End, options[0].Option);
    }
    
    [Fact]
    public void AddDnsSearchList_WithNullArray_ShouldNotThrow()
    {
        // Arrange
        var builder = new DhcpOptionsBuilder();
        
        // Act & Assert - Should not throw exception
        var result = builder.AddDnsSearchList(null);
        
        Assert.NotNull(result);
        var options = result.Build();
        
        // Should only contain the End option
        Assert.Single(options);
        Assert.Equal(EOption.End, options[0].Option);
    }
    
    [Fact]
    public void AddClasslessStaticRoutes_WithNullDictionary_ShouldNotThrow()
    {
        // Arrange
        var builder = new DhcpOptionsBuilder();
        
        // Act & Assert - Should not throw exception
        var result = builder.AddClasslessStaticRoutes(null);
        
        Assert.NotNull(result);
        var options = result.Build();
        
        // Should only contain the End option
        Assert.Single(options);
        Assert.Equal(EOption.End, options[0].Option);
    }
    
    [Fact]
    public void AddRouterOption_WithNullArray_ShouldThrowInvalidDataException()
    {
        // Arrange
        var builder = new DhcpOptionsBuilder();
        
        // Act & Assert - Router is required, so null should throw
        var exception = Assert.Throws<InvalidDataException>(() => builder.AddRouterOption(null));
        Assert.Equal("At least 1 router is required", exception.Message);
    }
}