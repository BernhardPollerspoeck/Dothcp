# Add Multi-Subnet Support

**Priority:** Medium

## Description

The current DHCP server supports only a single subnet configuration. Adding multi-subnet support will allow the server to operate in more complex network environments and serve clients on different network segments.

## Implementation Details

1. Create multi-subnet configuration:
   - Implement `DhcpScope` class for subnet definitions
   - Add properties for subnet-specific settings
   - Create storage for multiple scope configurations
   - Add scope identification and selection logic

2. Implement subnet selection logic:
   - Add logic to determine appropriate scope for client requests
   - Handle relay agent information for remote subnets
   - Create subnet-specific option sets
   - Implement fallback mechanisms

3. Add subnet management UI:
   - Create interface for managing multiple subnets
   - Implement add/edit/delete functionality for subnets
   - Add validation for subnet overlaps
   - Create scope-specific dashboard views

## Testing Criteria

- Verify server correctly identifies client subnet
- Test IP allocation from the correct subnet
- Confirm subnet-specific options are provided
- Test relay agent scenarios
- Verify subnet management UI functionality
- Test subnet selection with multiple overlapping subnets