using qt.qsp.dhcp.Server.Grains.DhcpManager;
using qt.qsp.dhcp.Server.Services.Core;
using System.Net;
using System.Text.Json;

namespace qt.qsp.dhcp.Server.Services;

public class ReservationService : IReservationService
{
    private readonly Core.IReservationService _reservationServiceCore;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(Core.IReservationService reservationServiceCore, ILogger<ReservationService> logger)
    {
        _reservationServiceCore = reservationServiceCore;
        _logger = logger;
    }

    public async Task<IList<DhcpReservation>> GetAllReservationsAsync()
    {
        try
        {
            var reservations = await _reservationServiceCore.GetAllReservationsAsync();
            // Convert from Models.DhcpReservation to Grains.DhcpManager.DhcpReservation
            return reservations.Select(ConvertToGrainModel).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all reservations");
            return new List<DhcpReservation>();
        }
    }

    public async Task<DhcpReservation?> GetReservationByMacAsync(string macAddress)
    {
        try
        {
            var reservation = await _reservationServiceCore.GetReservationByMacAsync(macAddress);
            return reservation != null ? ConvertToGrainModel(reservation) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation by MAC {MacAddress}", macAddress);
            return null;
        }
    }

    public async Task<DhcpReservation?> GetReservationByIpAsync(IPAddress ipAddress)
    {
        try
        {
            var reservation = await _reservationServiceCore.GetReservationByIpAsync(ipAddress);
            return reservation != null ? ConvertToGrainModel(reservation) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation by IP {IpAddress}", ipAddress);
            return null;
        }
    }

    public async Task<(bool success, string? errorMessage)> AddReservationAsync(DhcpReservation reservation)
    {
        try
        {
            var modelReservation = ConvertToModel(reservation);
            return await _reservationServiceCore.AddReservationAsync(modelReservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add reservation for MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string? errorMessage)> UpdateReservationAsync(DhcpReservation reservation)
    {
        try
        {
            var modelReservation = ConvertToModel(reservation);
            return await _reservationServiceCore.UpdateReservationAsync(modelReservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update reservation for MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string? errorMessage)> DeleteReservationAsync(IPAddress ipAddress)
    {
        try
        {
            return await _reservationServiceCore.DeleteReservationAsync(ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete reservation for IP {IpAddress}", ipAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool hasConflict, string? conflictReason)> CheckConflictAsync(DhcpReservation reservation)
    {
        try
        {
            var modelReservation = ConvertToModel(reservation);
            return await _reservationServiceCore.HasConflictAsync(modelReservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check conflict for reservation MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);
            return (true, "Error checking for conflicts");
        }
    }

    public async Task<DhcpReservation?> GetReservationForMacAsync(string macAddress)
    {
        try
        {
            var reservation = await _reservationServiceCore.GetReservationByMacAsync(macAddress);
            return reservation != null ? ConvertToGrainModel(reservation) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation for MAC {MacAddress}", macAddress);
            return null;
        }
    }

    public async Task<string> ExportReservationsAsJsonAsync()
    {
        try
        {
            var reservations = await GetAllReservationsAsync();
            var exportData = reservations.Select(r => new
            {
                IpAddress = r.IpAddress.ToString(),
                MacAddress = r.MacAddress,
                Description = r.Description,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                LastUsed = r.LastUsed
            }).ToList();

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export reservations");
            return "[]";
        }
    }

    public async Task<(bool success, string? errorMessage, int importedCount)> ImportReservationsFromJsonAsync(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return (false, "JSON data is empty", 0);
            }

            var importData = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (importData == null || importData.Length == 0)
            {
                return (false, "No reservation data found in JSON", 0);
            }

            var importedCount = 0;
            var errors = new List<string>();

            foreach (var item in importData)
            {
                try
                {
                    if (!item.TryGetProperty("IpAddress", out var ipProp) ||
                        !item.TryGetProperty("MacAddress", out var macProp))
                    {
                        errors.Add("Missing required IpAddress or MacAddress property");
                        continue;
                    }

                    if (!IPAddress.TryParse(ipProp.GetString(), out var ipAddress))
                    {
                        errors.Add($"Invalid IP address: {ipProp.GetString()}");
                        continue;
                    }

                    var reservation = new DhcpReservation
                    {
                        IpAddress = ipAddress,
                        MacAddress = macProp.GetString() ?? string.Empty,
                        Description = item.TryGetProperty("Description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty,
                        IsActive = item.TryGetProperty("IsActive", out var activeProp) ? activeProp.GetBoolean() : true,
                        CreatedAt = DateTime.UtcNow // Use current time for imports
                    };

                    // Check for conflicts before importing
                    var (hasConflict, conflictReason) = await CheckConflictAsync(reservation);
                    if (hasConflict)
                    {
                        errors.Add($"Conflict for {ipAddress}: {conflictReason}");
                        continue;
                    }

                    var (success, errorMessage) = await AddReservationAsync(reservation);
                    if (success)
                    {
                        importedCount++;
                    }
                    else
                    {
                        errors.Add($"Failed to import {ipAddress}: {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing reservation: {ex.Message}");
                }
            }

            if (errors.Any())
            {
                var errorMessage = $"Imported {importedCount} reservations with {errors.Count} errors: {string.Join("; ", errors)}";
                return (importedCount > 0, errorMessage, importedCount);
            }

            return (true, null, importedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import reservations from JSON");
            return (false, ex.Message, 0);
        }
    }

    // Helper methods to convert between Models.DhcpReservation and Grains.DhcpManager.DhcpReservation
    private Models.DhcpReservation ConvertToModel(DhcpReservation grainReservation)
    {
        return new Models.DhcpReservation
        {
            MacAddress = grainReservation.MacAddress,
            IpAddressString = grainReservation.IpAddressString,
            Description = grainReservation.Description,
            IsActive = grainReservation.IsActive,
            CreatedAt = grainReservation.CreatedAt,
            LastUsed = grainReservation.LastUsed,
            SubnetString = grainReservation.SubnetString,
            RouterString = grainReservation.RouterString,
            DhcpServerString = grainReservation.DhcpServerString,
            DnsServerStringsJson = grainReservation.DnsServerStrings.Count > 0
                ? JsonSerializer.Serialize(grainReservation.DnsServerStrings)
                : null
        };
    }

    private DhcpReservation ConvertToGrainModel(Models.DhcpReservation modelReservation)
    {
        var grainReservation = new DhcpReservation
        {
            MacAddress = modelReservation.MacAddress,
            IpAddressString = modelReservation.IpAddressString,
            Description = modelReservation.Description,
            IsActive = modelReservation.IsActive,
            CreatedAt = modelReservation.CreatedAt,
            LastUsed = modelReservation.LastUsed,
            SubnetString = modelReservation.SubnetString,
            RouterString = modelReservation.RouterString,
            DhcpServerString = modelReservation.DhcpServerString
        };

        if (!string.IsNullOrEmpty(modelReservation.DnsServerStringsJson))
        {
            var dnsServers = JsonSerializer.Deserialize<List<string>>(modelReservation.DnsServerStringsJson);
            if (dnsServers != null)
            {
                grainReservation.DnsServerStrings = dnsServers;
            }
        }

        return grainReservation;
    }
}