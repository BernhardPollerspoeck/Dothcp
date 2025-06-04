using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net;
using System.Text.Json;

namespace qt.qsp.dhcp.Server.Services;

public class ReservationService : IReservationService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(IGrainFactory grainFactory, ILogger<ReservationService> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public async Task<IList<DhcpReservation>> GetAllReservationsAsync()
    {
        try
        {
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetAllReservations();
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
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetReservationByMac(macAddress);
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
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetReservationByIp(ipAddress);
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
            // Validate the reservation
            if (string.IsNullOrWhiteSpace(reservation.MacAddress))
            {
                return (false, "MAC address is required");
            }

            if (string.IsNullOrEmpty(reservation.IpAddressString) || !IPAddress.TryParse(reservation.IpAddressString, out var _) || reservation.IpAddress.Equals(IPAddress.None))
            {
                return (false, "IP address is required");
            }

            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            var success = await managerGrain.AddReservation(reservation);
            
            if (success)
            {
                _logger.LogInformation("Successfully added reservation for MAC {MacAddress} -> IP {IpAddress}", 
                    reservation.MacAddress, reservation.IpAddress);
                return (true, null);
            }
            else
            {
                return (false, "Failed to add reservation - check for conflicts");
            }
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
            // Validate the reservation
            if (string.IsNullOrWhiteSpace(reservation.MacAddress))
            {
                return (false, "MAC address is required");
            }

            if (string.IsNullOrEmpty(reservation.IpAddressString) || !IPAddress.TryParse(reservation.IpAddressString, out var _) || reservation.IpAddress.Equals(IPAddress.None))
            {
                return (false, "IP address is required");
            }

            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            var success = await managerGrain.UpdateReservation(reservation);
            
            if (success)
            {
                _logger.LogInformation("Successfully updated reservation for MAC {MacAddress} -> IP {IpAddress}", 
                    reservation.MacAddress, reservation.IpAddress);
                return (true, null);
            }
            else
            {
                return (false, "Failed to update reservation");
            }
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
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            var success = await managerGrain.DeleteReservation(ipAddress);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted reservation for IP {IpAddress}", ipAddress);
                return (true, null);
            }
            else
            {
                return (false, "Failed to delete reservation");
            }
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
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.HasConflict(reservation);
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
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetReservationForMac(macAddress);
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
}