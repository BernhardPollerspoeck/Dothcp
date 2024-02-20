namespace qt.qsp.dhcp.Server.Models.Enumerations;

public enum EMessageType : byte
{
    Unknown = 0,
    Discover = 1,//client message to gather offers
    Offer = 2,//server message offer of a lease
    Request = 3,//client requests the ip of an offer

    Ack = 4,//server confirms the request
}