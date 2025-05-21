# Implement Dashboard Home Page

**Priority:** High

## Description

The home page of the Blazor UI is currently empty, with only boilerplate content. A functional dashboard is needed to display server status and important metrics for administrators.

## Implementation Details

1. Create server status display:
   - Implement uptime counter
   - Display current server state (running, stopped, error)
   - Add quick action buttons (start/stop/restart service)
   - Show current network interface information

2. Implement lease utilization statistics:
   - Create visual representation of IP address space usage
   - Display total available, leased, and reserved addresses
   - Add trend graph of lease utilization over time
   - Show utilization percentage

3. Add active lease metrics:
   - Display count of active leases
   - Show recent lease activities
   - Implement filterable lease table with key information
   - Add quick links to detailed lease information

## Testing Criteria

- Verify all dashboard components load properly
- Test dashboard updates when server status changes
- Confirm lease statistics accurately reflect system state
- Test responsiveness of dashboard UI on different screen sizes
- Verify performance with large numbers of leases