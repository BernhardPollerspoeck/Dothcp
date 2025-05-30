namespace qt.qsp.dhcp.Server.Models.Enumerations;

public enum EMessageType : byte
{
    Unknown = 0,
    Discover = 1,//client message to gather offers
    Offer = 2,//server message offer of a lease
    Request = 3,//client requests the ip of an offer
    
    Decline = 4,//client declines an offer
    Ack = 5,//server confirms the request
    Nak = 6,//server refuses a request
    Release = 7,//client releases an address
    Inform = 8//client requests network info only
}