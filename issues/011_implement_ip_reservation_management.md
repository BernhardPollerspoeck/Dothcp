# Implement IP Reservation Management

**Priority:** Medium

## Description

The DHCP server needs functionality to manage static IP reservations, allowing administrators to assign specific IP addresses to specific MAC addresses permanently.

## Implementation Details

1. Create reservation data structure:
   - Implement `DhcpReservation` class
   - Add properties for MAC address, IP address, description, and status
   - Create persistent storage for reservations
   - Implement reservation loading/saving

2. Add reservation management UI:
   - Create interface for viewing all reservations
   - Implement add/edit/delete functionality for reservations
   - Add validation for reservation conflicts
   - Create bulk import/export capabilities

3. Integrate with DHCP server logic:
   - Modify address allocation to check reservations first
   - Update DHCPOFFER and DHCPACK handlers to honor reservations
   - Implement priority handling between leases and reservations
   - Add reservation status tracking (active/inactive)

## Testing Criteria

- Verify reservations are properly saved and loaded
- Test reservation UI functionality (add, edit, delete)
- Confirm DHCP server correctly assigns reserved IPs to matching MAC addresses
- Verify conflict detection when adding reservations
- Test import/export functionality
- Confirm reservations take precedence over dynamic allocation