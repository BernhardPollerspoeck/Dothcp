# Create Active Leases View

**Priority:** High

## Description

The server needs a UI component to view and manage active DHCP leases. This interface will allow administrators to monitor and manipulate lease information.

## Implementation Details

1. Implement lease data table:
   - Create paginated table of current DHCP leases
   - Include columns for IP address, MAC address, hostname, lease start and expiration
   - Add sorting capability by different columns
   - Implement row selection for actions

2. Create lease details view:
   - Implement detailed view of selected lease
   - Display all lease parameters
   - Show lease history if available
   - Add client information

3. Add lease management actions:
   - Implement manual lease extension button
   - Add lease termination functionality
   - Create lease modification interface
   - Add confirmation for destructive actions

4. Implement filtering and search:
   - Add search box for finding specific leases
   - Create filters for lease status, time remaining, subnet
   - Implement filter combinations
   - Add saved search functionality

## Testing Criteria

- Verify lease table loads and displays data correctly
- Test pagination with large lease sets
- Confirm lease details display works
- Test lease management actions (extend, terminate)
- Verify search and filtering functionality
- Test UI responsiveness with different screen sizes